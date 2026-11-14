using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
                ["Authentication:Facebook:AppSecret"] = "integration-test-secret"
            });
        });
    }
}
