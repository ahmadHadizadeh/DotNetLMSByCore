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

    void ClearAccountAfterSuccessfulLogin(string email);

    void ClearAccountAfterSuccessfulPasswordReset(string email);
}
