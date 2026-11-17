namespace Core.Security;

public sealed class LoginRateLimitOptions
{
    public const string SectionName = "AuthenticationRateLimit";

    public string KeyPepper { get; set; } = string.Empty;

    public int AccountFailureThreshold { get; set; } = 8;
    public int AccountWindowMinutes { get; set; } = 15;
    public int AccountBaseCooldownMinutes { get; set; } = 1;
    public int AccountMaxCooldownMinutes { get; set; } = 15;

    public bool EnableIpRateLimit { get; set; }
    public int IpFailureThreshold { get; set; } = 60;
    public int IpWindowMinutes { get; set; } = 15;
    public int IpCooldownMinutes { get; set; } = 5;

    // When proxies are used, populate TrustedProxies with their exact IP addresses.
    // DirectClientConnections is an explicit assertion that the app receives client sockets directly.
    public bool DirectClientConnections { get; set; }
    public List<string> TrustedProxies { get; set; } = [];

    public int RetentionHours { get; set; } = 48;
}
