using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Core.Security.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Asancode.Tests.Infrastructure;

public sealed class MediaTestApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AsanCodeConnection"] = connectionString,
                ["Authentication:Google:ClientId"] = "integration-test-client",
                ["Authentication:Google:ClientSecret"] = "integration-test-secret",
                ["Authentication:Facebook:AppId"] = "integration-test-app",
                ["Authentication:Facebook:AppSecret"] = "integration-test-secret",
                ["MediaUpload:MaxArchiveBytes"] = "2048",
                ["AuthenticationRateLimit:KeyPepper"] = "dGVzdGluZy1vbmx5LXBlcHBlci1mb3Itc3FsLXNlcnZlci10ZXN0cy0yMDI2",
                ["AuthenticationRateLimit:AccountFailureThreshold"] = "8",
                ["AuthenticationRateLimit:AccountWindowMinutes"] = "15",
                ["AuthenticationRateLimit:AccountBaseCooldownMinutes"] = "1",
                ["AuthenticationRateLimit:AccountMaxCooldownMinutes"] = "15",
                ["AuthenticationRateLimit:EnableIpRateLimit"] = "true",
                ["AuthenticationRateLimit:IpFailureThreshold"] = "60",
                ["AuthenticationRateLimit:IpWindowMinutes"] = "15",
                ["AuthenticationRateLimit:IpCooldownMinutes"] = "5",
                ["AuthenticationRateLimit:DirectClientConnections"] = "true",
                ["AuthenticationRateLimit:RetentionHours"] = "48"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<RecordingEmailSender>();
            services.AddSingleton<IEmailSender>(provider =>
                provider.GetRequiredService<RecordingEmailSender>());

            const string testOrCookieScheme = "MediaTestOrCookie";

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = testOrCookieScheme;
                    options.DefaultAuthenticateScheme = testOrCookieScheme;
                    options.DefaultChallengeScheme = testOrCookieScheme;
                    options.DefaultForbidScheme = testOrCookieScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, MediaTestAuthenticationHandler>(
                    MediaTestAuthenticationHandler.SchemeName,
                    _ => { })
                .AddPolicyScheme(testOrCookieScheme, "Media test header or application cookie", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        if (context.Request.Headers.ContainsKey(MediaTestAuthenticationHandler.UserHeaderName))
                        {
                            return MediaTestAuthenticationHandler.SchemeName;
                        }

                        return string.Equals(
                            context.Request.Headers[MediaTestAuthenticationHandler.UseRealCookieHeaderName],
                            "true",
                            StringComparison.OrdinalIgnoreCase)
                                ? CookieAuthenticationDefaults.AuthenticationScheme
                                : MediaTestAuthenticationHandler.SchemeName;
                    };
                });
        });
    }
}
