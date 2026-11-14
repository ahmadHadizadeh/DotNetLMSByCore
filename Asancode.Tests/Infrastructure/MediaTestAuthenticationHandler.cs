using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Asancode.Tests.Infrastructure;

public sealed class MediaTestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "MediaTest";
    public const string UserHeaderName = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userName = Request.Headers[UserHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.NameIdentifier, userName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
