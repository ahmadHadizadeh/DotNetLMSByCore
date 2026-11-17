namespace DataLayer.Entities.User;

public sealed class AuthenticationRateLimitEntry
{
    /// <summary>1 = account identifier, 2 = client IP address.</summary>
    public byte Scope { get; set; }

    /// <summary>HMAC-SHA256 digest; the original account identifier or IP is never persisted.</summary>
    public string KeyHash { get; set; } = string.Empty;

    public int FailedAttempts { get; set; }

    public DateTime WindowStartedUtc { get; set; }

    public DateTime? BlockedUntilUtc { get; set; }

    public int ConsecutiveLockouts { get; set; }

    public DateTime LastAttemptUtc { get; set; }
}
