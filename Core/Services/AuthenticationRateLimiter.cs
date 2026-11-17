using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Core.Convertors;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Services;

public sealed class AuthenticationRateLimiter : IAuthenticationRateLimiter
{
    private const byte AccountScope = 1;
    private const byte IpScope = 2;
    private const byte AccountCapacityScope = 3;

    private readonly AsanwebContext _context;
    private readonly LoginRateLimitOptions _options;
    private readonly byte[] _keyPepper;
    private readonly TimeProvider _timeProvider;

    private readonly LimitPolicy _accountPolicy;
    private readonly LimitPolicy _ipPolicy;

    public AuthenticationRateLimiter(
        AsanwebContext context,
        IOptions<LoginRateLimitOptions> options,
        TimeProvider timeProvider)
    {
        _context = context;
        _options = options.Value;
        _timeProvider = timeProvider;
        _keyPepper = Convert.FromBase64String(_options.KeyPepper);

        if (_keyPepper.Length < 32)
        {
            throw new InvalidOperationException(
                "Authentication rate-limit key pepper must contain at least 32 random bytes.");
        }

        _accountPolicy = new LimitPolicy(
            _options.AccountFailureThreshold,
            TimeSpan.FromMinutes(_options.AccountWindowMinutes),
            _options.AccountBaseCooldownMinutes,
            _options.AccountMaxCooldownMinutes,
            IsProgressive: true);

        _ipPolicy = new LimitPolicy(
            _options.IpFailureThreshold,
            TimeSpan.FromMinutes(_options.IpWindowMinutes),
            _options.IpCooldownMinutes,
            _options.IpCooldownMinutes,
            IsProgressive: false);
    }

    public LoginRateLimitStatus GetStatus(string email, IPAddress? clientIpAddress)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var accountKey = CreateAccountKey(email);
        var accountEntry = FindEntry(AccountScope, accountKey);

        // The capacity gate applies to every previously untracked identifier, regardless
        // of whether that identifier belongs to a registered user. This keeps the public
        // login response independent of account existence.
        if (accountKey is not null && accountEntry is null)
        {
            var capacityEntry = FindEntry(AccountCapacityScope, CreateAccountCapacityKey());
            if (GetRemainingSeconds(capacityEntry, now) > 0)
            {
                accountEntry = capacityEntry;
            }
        }

        var ipEntry = FindIpEntry(clientIpAddress);
        return CombineStatus(accountEntry, ipEntry, now);
    }

    public LoginRateLimitStatus RecordFailedAttempt(string email, IPAddress? clientIpAddress)
    {
        var accountKey = CreateAccountKey(email);
        var ipKey = CreateIpKey(clientIpAddress);

        if (accountKey is null && ipKey is null)
        {
            return LoginRateLimitStatus.Allowed;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);

        // Keep lock acquisition order stable: account key, IP key, then the shared
        // capacity gate. Existing buckets are locked before their state is inspected.
        var accountEntry = accountKey is null
            ? null
            : LoadExistingForUpdate(AccountScope, accountKey);
        var ipEntry = ipKey is null
            ? null
            : LoadExistingForUpdate(IpScope, ipKey);

        var existingLimit = CombineStatus(accountEntry, ipEntry, now);
        if (existingLimit.IsLimited)
        {
            transaction.Commit();
            return existingLimit;
        }

        if (accountKey is not null && accountEntry is null)
        {
            // This single SQL row serializes all new account-bucket admissions across
            // application instances. The count and insert therefore share one lock.
            var capacityEntry = LoadOrCreateForUpdate(
                AccountCapacityScope,
                CreateAccountCapacityKey(),
                now);

            var capacityLimit = CombineStatus(capacityEntry, ipEntry, now);
            if (capacityLimit.IsLimited)
            {
                transaction.Commit();
                return capacityLimit;
            }

            var trackedAccountBuckets = _context.AuthenticationRateLimitEntries
                .Count(entry => entry.Scope == AccountScope);

            if (trackedAccountBuckets >= _options.MaxAccountBuckets)
            {
                capacityEntry.BlockedUntilUtc =
                    now.AddMinutes(_options.AccountBucketCapacityRetryMinutes);
                capacityEntry.LastAttemptUtc = now;

                _context.SaveChanges();
                transaction.Commit();
                return CombineStatus(capacityEntry, ipEntry, now);
            }

            accountEntry = new AuthenticationRateLimitEntry
            {
                Scope = AccountScope,
                KeyHash = accountKey,
                FailedAttempts = 0,
                WindowStartedUtc = now,
                BlockedUntilUtc = null,
                ConsecutiveLockouts = 0,
                LastAttemptUtc = now
            };
            _context.AuthenticationRateLimitEntries.Add(accountEntry);
        }

        if (ipKey is not null && ipEntry is null)
        {
            ipEntry = LoadOrCreateForUpdate(IpScope, ipKey, now);
        }

        if (accountEntry is not null)
        {
            ApplyFailure(accountEntry, _accountPolicy, now);
        }

        if (ipEntry is not null)
        {
            ApplyFailure(ipEntry, _ipPolicy, now);
        }

        _context.SaveChanges();
        transaction.Commit();

        return CombineStatus(accountEntry, ipEntry, now);
    }

    public void ClearAccountAfterSuccessfulLogin(string email)
    {
        // A successful account login must not reset the independent shared IP budget.
        ClearEntries((AccountScope, CreateAccountKey(email)));
    }

    public void ClearAccountAfterSuccessfulPasswordReset(string email)
    {
        ClearEntries((AccountScope, CreateAccountKey(email)));
    }

    private AuthenticationRateLimitEntry? FindEntry(byte scope, string? keyHash)
    {
        if (keyHash is null)
        {
            return null;
        }

        return _context.AuthenticationRateLimitEntries
            .AsNoTracking()
            .SingleOrDefault(entry => entry.Scope == scope && entry.KeyHash == keyHash);
    }

    private AuthenticationRateLimitEntry? FindIpEntry(IPAddress? address)
    {
        return _options.EnableIpRateLimit
            ? FindEntry(IpScope, CreateIpKey(address))
            : null;
    }

    private AuthenticationRateLimitEntry LoadOrCreateForUpdate(
        byte scope,
        string keyHash,
        DateTime now)
    {
        // UPDLOCK + HOLDLOCK acquires an update/range lock on the composite primary key.
        // This serializes simultaneous inserts and increments across app instances.
        var entry = _context.AuthenticationRateLimitEntries
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[AuthenticationRateLimitEntries] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Scope] = {scope} AND [KeyHash] = CONVERT(char(64), {keyHash})
                """)
            .SingleOrDefault();

        if (entry is not null)
        {
            return entry;
        }

        entry = new AuthenticationRateLimitEntry
        {
            Scope = scope,
            KeyHash = keyHash,
            FailedAttempts = 0,
            WindowStartedUtc = now,
            BlockedUntilUtc = null,
            ConsecutiveLockouts = 0,
            LastAttemptUtc = now
        };

        _context.AuthenticationRateLimitEntries.Add(entry);
        return entry;
    }

    private AuthenticationRateLimitEntry? LoadExistingForUpdate(byte scope, string? keyHash)
    {
        if (keyHash is null)
        {
            return null;
        }

        return _context.AuthenticationRateLimitEntries
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[AuthenticationRateLimitEntries] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Scope] = {scope} AND [KeyHash] = CONVERT(char(64), {keyHash})
                """)
            .SingleOrDefault();
    }

    private void ClearEntries(params (byte Scope, string? KeyHash)[] keys)
    {
        var existingKeys = keys
            .Where(key => key.KeyHash is not null)
            .Distinct()
            .ToArray();

        if (existingKeys.Length == 0)
        {
            return;
        }

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);

        foreach (var key in existingKeys)
        {
            var entry = LoadExistingForUpdate(key.Scope, key.KeyHash);
            if (entry is not null)
            {
                _context.AuthenticationRateLimitEntries.Remove(entry);
            }
        }

        _context.SaveChanges();
        transaction.Commit();
    }

    private static void ApplyFailure(
        AuthenticationRateLimitEntry entry,
        LimitPolicy policy,
        DateTime now)
    {
        if (entry.BlockedUntilUtc is DateTime blockedUntil)
        {
            if (blockedUntil > now)
            {
                return;
            }

            // The cooldown expired. Preserve the escalation level, but start a fresh
            // observation window for the next group of failed attempts.
            entry.BlockedUntilUtc = null;
            entry.FailedAttempts = 0;
            entry.WindowStartedUtc = now;
        }

        if (now - entry.WindowStartedUtc >= policy.Window)
        {
            entry.FailedAttempts = 0;
            entry.WindowStartedUtc = now;
        }

        entry.FailedAttempts++;
        entry.LastAttemptUtc = now;

        if (entry.FailedAttempts < policy.FailureThreshold)
        {
            return;
        }

        entry.FailedAttempts = 0;
        entry.WindowStartedUtc = now;
        entry.ConsecutiveLockouts = policy.IsProgressive
            ? Math.Min(entry.ConsecutiveLockouts + 1, 21)
            : 1;

        var multiplier = policy.IsProgressive
            ? 1L << Math.Min(entry.ConsecutiveLockouts - 1, 20)
            : 1L;
        var cooldownMinutes = Math.Min(
            (long)policy.BaseCooldownMinutes * multiplier,
            policy.MaxCooldownMinutes);

        entry.BlockedUntilUtc = now.AddMinutes(cooldownMinutes);
    }

    private static LoginRateLimitStatus CombineStatus(
        AuthenticationRateLimitEntry? accountEntry,
        AuthenticationRateLimitEntry? ipEntry,
        DateTime now)
    {
        var accountSeconds = GetRemainingSeconds(accountEntry, now);
        var ipSeconds = GetRemainingSeconds(ipEntry, now);
        var remaining = Math.Max(accountSeconds, ipSeconds);

        return remaining > 0
            ? new LoginRateLimitStatus(true, remaining)
            : LoginRateLimitStatus.Allowed;
    }

    private static int GetRemainingSeconds(AuthenticationRateLimitEntry? entry, DateTime now)
    {
        if (entry?.BlockedUntilUtc is not DateTime blockedUntil || blockedUntil <= now)
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling((blockedUntil - now).TotalSeconds));
    }

    private string CreateAccountCapacityKey() =>
        CreateKeyHash("account-capacity", "global");

    private string? CreateAccountKey(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = FixedTexd.FixedEmail(email);
        return CreateKeyHash("account", normalizedEmail);
    }

    private string? CreateIpKey(IPAddress? clientIpAddress)
    {
        if (!_options.EnableIpRateLimit || clientIpAddress is null)
        {
            return null;
        }

        var normalizedAddress = clientIpAddress.IsIPv4MappedToIPv6
            ? clientIpAddress.MapToIPv4()
            : clientIpAddress;

        return CreateKeyHash("ip", normalizedAddress.ToString());
    }

    private string CreateKeyHash(string scope, string value)
    {
        var input = Encoding.UTF8.GetBytes(scope + "\n" + value);
        return Convert.ToHexString(HMACSHA256.HashData(_keyPepper, input));
    }

    private readonly record struct LimitPolicy(
        int FailureThreshold,
        TimeSpan Window,
        int BaseCooldownMinutes,
        int MaxCooldownMinutes,
        bool IsProgressive);
}
