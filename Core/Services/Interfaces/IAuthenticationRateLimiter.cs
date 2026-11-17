using System.Net;

namespace Core.Services.Interfaces;

public sealed record LoginRateLimitStatus(bool IsLimited, int RetryAfterSeconds)
{
    public static LoginRateLimitStatus Allowed { get; } = new(false, 0);
}

public interface IAuthenticationRateLimiter
{
    LoginRateLimitStatus GetStatus(string email, IPAddress? clientIpAddress);

    LoginRateLimitStatus RecordFailedAttempt(string email, IPAddress? clientIpAddress);

    void ClearAfterSuccessfulLogin(string email, IPAddress? clientIpAddress);

    void ClearAccountAfterSuccessfulPasswordReset(string email);
}
