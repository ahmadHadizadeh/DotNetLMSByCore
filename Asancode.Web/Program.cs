using AngleSharp;
using Asancode.Web.ExterNalProvider;
using Asancode.Web.Options;
using Core.Convertors;
using Core.Services;
using Core.Services.Interfaces;
using DataLayer.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

#region Services

// MVC
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
builder.Services.AddRazorPages();

// IOC
builder.Services.AddTransient<IUserService, Userservice>();
builder.Services.AddTransient<IViewRenderService, RenderViewToString>();
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
.AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(43200);
})
.AddGoogle(option =>
{
    option.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    option.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
});

// Form limits
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = maxMediaArchiveBytes;
});

#endregion

var app = builder.Build();

#region Middleware

// 404 handler
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404)
    {
        context.Response.Redirect("/Home/Error404");
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error404");
    app.UseHsts();
}

// Give the Teacher upload endpoint its configured request-body limit,
// including a bounded allowance for multipart headers and boundaries.
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals(
            "/UserPanel/Teacher/DropzoneTarget",
            StringComparison.OrdinalIgnoreCase))
    {
        var requestSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSizeFeature is { IsReadOnly: false })
        {
            requestSizeFeature.MaxRequestBodySize = checked(maxMediaArchiveBytes + 64 * 1024);
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
