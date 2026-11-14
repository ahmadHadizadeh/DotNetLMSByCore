using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                ["MediaUpload:MaxArchiveBytes"] = "2048"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = MediaTestAuthenticationHandler.SchemeName;
                    options.DefaultAuthenticateScheme = MediaTestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = MediaTestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = MediaTestAuthenticationHandler.SchemeName;
                    options.DefaultSignInScheme = MediaTestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, MediaTestAuthenticationHandler>(
                    MediaTestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }
}
