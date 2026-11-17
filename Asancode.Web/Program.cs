using AngleSharp;
using Asancode.Web.ExterNalProvider;
using Asancode.Web.Options;
using Core.Convertors;
using Core.Security;
using Core.Security.Sender;
using Core.Services;
using Core.Services.Interfaces;
using DataLayer.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Development-only fallback: never commit a reusable HMAC pepper to source control.
// Production and other persistent environments must supply a stable secret via configuration.
var rateLimitPepperConfigurationKey = $"{LoginRateLimitOptions.SectionName}:KeyPepper";
if (builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration[rateLimitPepperConfigurationKey]))
{
    builder.Configuration[rateLimitPepperConfigurationKey] =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

#region Services

// MVC
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
builder.Services.AddRazorPages();

// IOC
builder.Services.AddTransient<UserPasswordHasher>();
builder.Services.AddTransient<IUserService, Userservice>();

// Login rate limiting uses a stable application secret to create non-reversible account/IP keys.
var loginRateLimitOptions = builder.Configuration
    .GetSection(LoginRateLimitOptions.SectionName)
    .Get<LoginRateLimitOptions>() ?? new LoginRateLimitOptions();

builder.Services
    .AddOptions<LoginRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(LoginRateLimitOptions.SectionName))
    .Validate(options =>
    {
        try
        {
            return Convert.FromBase64String(options.KeyPepper).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }, "AuthenticationRateLimit:KeyPepper must be a Base64-encoded secret of at least 32 bytes.")
    .Validate(options =>
        options.AccountFailureThreshold is > 0 and <= 100
        && options.AccountWindowMinutes is > 0 and <= 1440
        && options.AccountBaseCooldownMinutes is > 0 and <= 15
        && options.AccountMaxCooldownMinutes >= options.AccountBaseCooldownMinutes
        && options.AccountMaxCooldownMinutes <= 1440
        && options.IpFailureThreshold is > 0 and <= 100000
        && options.IpWindowMinutes is > 0 and <= 1440
        && options.IpCooldownMinutes is > 0 and <= 1440
        && options.RetentionHours is >= 1 and <= 720,
        "Authentication rate-limit thresholds and durations must be positive and internally consistent.")
    .ValidateOnStart();

var trustedProxyAddresses = new List<IPAddress>();
foreach (var proxyValue in loginRateLimitOptions.TrustedProxies)
{
    if (!IPAddress.TryParse(proxyValue, out var proxyAddress))
    {
        throw new InvalidOperationException(
            $"AuthenticationRateLimit:TrustedProxies contains an invalid IP address: '{proxyValue}'.");
    }

    trustedProxyAddresses.Add(proxyAddress);
}

if (loginRateLimitOptions.EnableIpRateLimit
    && trustedProxyAddresses.Count == 0
    && !loginRateLimitOptions.DirectClientConnections)
{
    throw new InvalidOperationException(
        "EnableIpRateLimit requires trusted proxy addresses or an explicit DirectClientConnections=true setting.");
}

if (loginRateLimitOptions.EnableIpRateLimit && trustedProxyAddresses.Count > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        foreach (var proxyAddress in trustedProxyAddresses)
        {
            options.KnownProxies.Add(proxyAddress);
        }
    });
}

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IAuthenticationRateLimiter, AuthenticationRateLimiter>();
builder.Services.AddHostedService<AuthenticationRateLimitCleanupService>();
builder.Services.AddTransient<IViewRenderService, RenderViewToString>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddTransient<IPermissionService, PermissionService>();
builder.Services.AddTransient<ICourseGroupService, CourseGroupService>();
builder.Services.AddTransient<IBlogService, BlogService>();
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddTransient<IForumService, ForumService>();

// Media upload limits
var maxMediaArchiveBytes = builder.Configuration.GetValue<long?>(
    $"{MediaUploadOptions.SectionName}:MaxArchiveBytes")
    ?? MediaUploadOptions.DefaultMaxArchiveBytes;

if (maxMediaArchiveBytes <= 0)
{
    throw new InvalidOperationException("Media upload size limit must be greater than zero.");
}

var maxMediaMultipartBytes = checked(maxMediaArchiveBytes + 64 * 1024);

builder.Services
    .AddOptions<MediaUploadOptions>()
    .Bind(builder.Configuration.GetSection(MediaUploadOptions.SectionName))
    .Validate(options => options.MaxArchiveBytes > 0,
        "Media upload size limit must be greater than zero.")
    .ValidateOnStart();

// DbContext
builder.Services.AddDbContext<AsanwebContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AsanCodeConnection"),
        x => x.MigrationsAssembly("DataLayer"));
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AsanwebContext>()
    .AddDefaultTokenProviders();

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    options.Cookie.Name = ".AsanCode.Authentication";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;

    // Validate each cookie against the active account's current security stamp.
    // Each security-sensitive update writes a fresh random stamp in the same SQL statement.
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var securityStampValue = context.Principal?.FindFirst(UserSessionClaims.SecurityStamp)?.Value;

            if (!int.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                || !Guid.TryParseExact(securityStampValue, "N", out var securityStamp)
                || securityStamp == Guid.Empty)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AsanwebContext>();
            var currentSecurityStamp = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.UserId == userId && user.IsActive)
                .Select(user => (Guid?)user.SecurityStamp)
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (currentSecurityStamp != securityStamp)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    };
});

// Form limits
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
});

#endregion

var app = builder.Build();

#region Middleware

// Resolve client IPs only through explicitly configured trusted proxies.
// Without that configuration, forwarded headers are ignored.
if (loginRateLimitOptions.EnableIpRateLimit && trustedProxyAddresses.Count > 0)
{
    app.UseForwardedHeaders();
}

// Redirect browser-facing HTML 404 responses to the legacy error page.
// Preserve the actual 404 status for JSON/XHR and other non-HTML clients.
app.Use(async (context, next) =>
{
    await next();

    var acceptsHtml = context.Request.Headers.Accept
        .ToString()
        .Contains("text/html", StringComparison.OrdinalIgnoreCase);

    if (context.Response.StatusCode == StatusCodes.Status404NotFound && acceptsHtml)
    {
        context.Response.Redirect("/Home/Error404");
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error404");
    app.UseHsts();
}

// Apply the archive request-body limit only to Media upload endpoints.
// The allowance covers multipart boundaries and form headers.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isTeacherUpload = path.Equals(
        "/UserPanel/Teacher/DropzoneTarget",
        StringComparison.OrdinalIgnoreCase);
    var isAdminCreateUpload = path.StartsWithSegments(
        "/Admin/Course/CreateEpisode",
        StringComparison.OrdinalIgnoreCase);
    var isAdminEditUpload = path.StartsWithSegments(
        "/Admin/Course/EditEpisode",
        StringComparison.OrdinalIgnoreCase);

    if (isTeacherUpload || isAdminCreateUpload || isAdminEditUpload)
    {
        var requestSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSizeFeature is { IsReadOnly: false })
        {
            requestSizeFeature.MaxRequestBodySize = maxMediaMultipartBytes;
        }
    }

    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware (FilePrice)
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.ToLower().StartsWith("/fileprice") == true)
    {
        var referer = context.Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) &&
            referer.StartsWith("https://asancode.com"))
        {
            await next();
        }
        else
        {
            context.Response.Redirect("/login");
        }
    }
    else
    {
        await next();
    }
});

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Razor Pages are registered above and must also be mapped as endpoints.
app.MapRazorPages();

#endregion

app.Run();

public partial class Program { }
