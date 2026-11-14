using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Asancode.Tests;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // The payment endpoint under test must not open a database connection.
                ["ConnectionStrings:AsanCodeConnection"] =
                    "Server=localhost;Database=Asancode_IntegrationTests;User Id=test_user;Password=NotARealCredential!;TrustServerCertificate=True;Encrypt=False;",
                ["Authentication:Google:ClientId"] = "integration-test-client",
                ["Authentication:Google:ClientSecret"] = "integration-test-secret",
                ["Authentication:Facebook:AppId"] = "integration-test-app",
                ["Authentication:Facebook:AppSecret"] = "integration-test-secret"
            });
        });
    }
}

public sealed class LegacyPaymentEndpointTests(TestApplicationFactory factory)
    : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Theory]
    [InlineData("/OnlinePayment/271?Status=OK&Authority=FAKE-AUTHORITY")]
    [InlineData("/OnlinePayment/271?Status=NOK")]
    public async Task Legacy_payment_endpoint_stays_disabled_and_uncacheable(string requestPath)
    {
        using var response = await _client.GetAsync(requestPath);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }
}
