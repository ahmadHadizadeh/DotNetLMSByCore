using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Asancode.Tests.Infrastructure;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Asancode.Tests;

public sealed class LoginRateLimitingIntegrationTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    [Fact]
    public async Task Repeated_failed_login_returns_429_and_renders_friendly_live_countdown()
    {
        var user = await SeedUserAsync();
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            using var response = await client.PostAsync(
                "/login",
                CreateForm(
                    ("Email", user.Email),
                    ("Password", "Wrong-Password-2026!"),
                    ("__RequestVerificationToken", antiforgeryToken)),
                TestContext.Current.CancellationToken);

            Assert.Equal(
                attempt == 8 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK,
                response.StatusCode);

            if (attempt == 8)
            {
                Assert.True(response.Headers.TryGetValues("Retry-After", out var retryHeaders));
                var retryAfter = int.Parse(Assert.Single(retryHeaders!));
                Assert.InRange(retryAfter, 1, 60);

                var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.Contains("به‌دلیل تلاش‌های ناموفق اخیر، امکان ورود موقتاً محدود شده است.", html);
                Assert.Contains("id=\"login-rate-limit-countdown\"", html);
                Assert.Contains("data-seconds=\"" + retryAfter + "\"", html);
                Assert.Contains("performance.now()", html);
                Assert.Contains("id=\"login-submit\"", html);
                Assert.Contains("disabled", html);
            }
        }

        // Spoofed forwarding headers cannot change the account bucket or bypass its lock.
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.17");
        using var blockedResponse = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Wrong-Password-2026!"),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);
    }


    [Fact]
    public async Task Ip_limit_is_enforced_through_http_and_untrusted_forwarded_headers_do_not_override_client_ip()
    {
        var users = await SeedUsersAsync(60);
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Remote-IP", "192.0.2.123");
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        for (var attempt = 1; attempt <= users.Count; attempt++)
        {
            using var response = await client.PostAsync(
                "/login",
                CreateForm(
                    ("Email", users[attempt - 1].Email),
                    ("Password", "Wrong-Password-2026!"),
                    ("__RequestVerificationToken", antiforgeryToken)),
                TestContext.Current.CancellationToken);

            Assert.Equal(
                attempt == 60 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK,
                response.StatusCode);

            if (attempt == 60)
            {
                Assert.True(response.Headers.TryGetValues("Retry-After", out var retryHeaders));
                var retryAfter = int.Parse(Assert.Single(retryHeaders!));
                Assert.InRange(retryAfter, 1, 300);
            }
        }

        // The test connection is explicitly assigned a known remote address.
        // An untrusted X-Forwarded-For header must not replace that address.
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.88");
        var anotherUser = await SeedUserAsync();
        using var blockedResponse = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", anotherUser.Email),
                ("Password", "Wrong-Password-2026!"),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);
    }

    [Fact]
    public async Task Cleanup_deletes_expired_entries_in_bounded_batches_and_preserves_active_cooldowns()
    {
        const int batchSize = 10;
        const int maxBatches = 2;
        var now = DateTime.UtcNow;
        var expiredKeys = Enumerable.Range(0, 25)
            .Select(index => Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes($"cleanup-{Guid.NewGuid():N}-{index}"))))
            .ToArray();
        var activeKey = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"cleanup-active-{Guid.NewGuid():N}")));
        var allKeys = expiredKeys.Append(activeKey).ToArray();

        await using (var seedScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AsanwebContext>();
            db.AuthenticationRateLimitEntries.AddRange(
                expiredKeys.Select(key => new AuthenticationRateLimitEntry
                {
                    Scope = 1,
                    KeyHash = key,
                    FailedAttempts = 1,
                    WindowStartedUtc = now.AddHours(-3),
                    BlockedUntilUtc = null,
                    ConsecutiveLockouts = 0,
                    LastAttemptUtc = now.AddHours(-3)
                }));
            db.AuthenticationRateLimitEntries.Add(new AuthenticationRateLimitEntry
            {
                Scope = 1,
                KeyHash = activeKey,
                FailedAttempts = 0,
                WindowStartedUtc = now.AddDays(-3),
                BlockedUntilUtc = now.AddMinutes(10),
                ConsecutiveLockouts = 1,
                LastAttemptUtc = now.AddDays(-3)
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        try
        {
            var cleanupOptions = Options.Create(new LoginRateLimitOptions
            {
                KeyPepper = "dGVzdC1rZXktcGVwcGVyLWZvci1pbnRlZ3JhdGlvbi10ZXN0cy0yMDI2LTMyLWJ5dGVz",
                CleanupBatchSize = batchSize,
                CleanupMaxBatchesPerRun = maxBatches
            });
            var cleanup = new AuthenticationRateLimitCleanupService(
                fixture.Factory.Services.GetRequiredService<IServiceScopeFactory>(),
                cleanupOptions,
                NullLogger<AuthenticationRateLimitCleanupService>.Instance,
                TimeProvider.System);

            var deleted = await cleanup.CleanupExpiredEntriesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(batchSize * maxBatches, deleted);

            await using var verifyScope = fixture.Factory.Services.CreateAsyncScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AsanwebContext>();
            var remainingExpired = await verifyDb.AuthenticationRateLimitEntries
                .CountAsync(entry => expiredKeys.Contains(entry.KeyHash), TestContext.Current.CancellationToken);
            var activeStillExists = await verifyDb.AuthenticationRateLimitEntries
                .AnyAsync(entry => entry.KeyHash == activeKey, TestContext.Current.CancellationToken);

            Assert.Equal(5, remainingExpired);
            Assert.True(activeStillExists);
        }
        finally
        {
            await using var cleanupScope = fixture.Factory.Services.CreateAsyncScope();
            var db = cleanupScope.ServiceProvider.GetRequiredService<AsanwebContext>();
            await db.AuthenticationRateLimitEntries
                .Where(entry => allKeys.Contains(entry.KeyHash))
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Account_bucket_uses_hmac_normalized_email_and_escalates_cooldown()
    {
        const string email = "  Rate-Limit-Case@Example.Test ";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var limiter = scope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            limiter.RecordFailedAttempt(email, null);
        }

        var firstLock = limiter.GetStatus("rate-limit-case@example.test", null);
        Assert.True(firstLock.IsLimited);
        Assert.InRange(firstLock.RetryAfterSeconds, 1, 60);

        var keyHash = GetAccountKeyHash("rate-limit-case@example.test");
        await using (var verifyScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AsanwebContext>();
            var entry = await db.AuthenticationRateLimitEntries
                .SingleAsync(
                    item => item.Scope == 1 && item.KeyHash == keyHash,
                    TestContext.Current.CancellationToken);
            Assert.DoesNotContain("rate-limit-case@example.test", keyHash, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, entry.ConsecutiveLockouts);
        }

        await using (var expireScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = expireScope.ServiceProvider.GetRequiredService<AsanwebContext>();
            await db.AuthenticationRateLimitEntries
                .Where(entry => entry.Scope == 1 && entry.KeyHash == keyHash)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        entry => entry.BlockedUntilUtc,
                        DateTime.UtcNow.AddSeconds(-1)),
                    TestContext.Current.CancellationToken);
        }

        await using (var secondScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var secondLimiter = secondScope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();
            for (var attempt = 1; attempt <= 8; attempt++)
            {
                secondLimiter.RecordFailedAttempt("rate-limit-case@example.test", null);
            }

            var secondLock = secondLimiter.GetStatus(email, null);
            Assert.True(secondLock.IsLimited);
            Assert.InRange(secondLock.RetryAfterSeconds, 60, 120);
        }

        await using var finalScope = fixture.Factory.Services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var finalEntry = await finalDb.AuthenticationRateLimitEntries
            .SingleAsync(
                item => item.Scope == 1 && item.KeyHash == keyHash,
                TestContext.Current.CancellationToken);
        Assert.Equal(2, finalEntry.ConsecutiveLockouts);
    }

    [Fact]
    public async Task Concurrent_failures_from_independent_scopes_share_one_atomic_account_bucket()
    {
        var email = $"concurrent-{Guid.NewGuid():N}@example.test";

        var tasks = Enumerable.Range(0, 24).Select(_ => Task.Run(() =>
        {
            using var scope = fixture.Factory.Services.CreateScope();
            var limiter = scope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();
            return limiter.RecordFailedAttempt(email, null);
        }));

        var results = await Task.WhenAll(tasks);
        Assert.Contains(results, result => result.IsLimited);

        await using var verifyScope = fixture.Factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var keyHash = GetAccountKeyHash(email);
        var matchingEntry = await db.AuthenticationRateLimitEntries
            .SingleAsync(
                entry => entry.Scope == 1 && entry.KeyHash == keyHash,
                TestContext.Current.CancellationToken);
        Assert.Equal(0, matchingEntry.FailedAttempts);
        Assert.Equal(1, matchingEntry.ConsecutiveLockouts);
        using var statusScope = fixture.Factory.Services.CreateScope();
        var statusLimiter = statusScope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();
        Assert.True(statusLimiter.GetStatus(email, null).IsLimited);
    }

    [Fact]
    public async Task Ip_bucket_is_independent_of_account_buckets_and_other_ip_addresses()
    {
        var firstAddress = IPAddress.Parse("203.0.113.71");
        var secondAddress = IPAddress.Parse("203.0.113.72");

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var limiter = scope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();

        for (var attempt = 1; attempt <= 60; attempt++)
        {
            limiter.RecordFailedAttempt($"ip-bucket-{attempt}@example.test", firstAddress);
        }

        Assert.True(limiter.GetStatus("unrelated-account@example.test", firstAddress).IsLimited);
        Assert.False(limiter.GetStatus("unrelated-account@example.test", secondAddress).IsLimited);
    }

    [Fact]
    public async Task Successful_login_clears_account_bucket_but_preserves_shared_ip_budget()
    {
        var user = await SeedUserAsync();
        var address = IPAddress.Parse("203.0.113.73");
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var limiter = scope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            limiter.RecordFailedAttempt(user.Email, address);
        }

        Assert.True(limiter.GetStatus(user.Email, address).IsLimited);

        limiter.ClearAccountAfterSuccessfulLogin(user.Email);

        Assert.False(limiter.GetStatus(user.Email, address).IsLimited);

        // The successful account login must not erase the shared IP budget.
        for (var attempt = 9; attempt <= 60; attempt++)
        {
            limiter.RecordFailedAttempt($"other-account-{attempt}@example.test", address);
        }

        Assert.True(limiter.GetStatus("unrelated-account@example.test", address).IsLimited);
    }

    [Fact]
    public async Task Successful_password_reset_clears_the_account_rate_limit_bucket()
    {
        var user = await SeedUserAsync();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var limiter = scope.ServiceProvider.GetRequiredService<IAuthenticationRateLimiter>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            limiter.RecordFailedAttempt(user.Email, null);
        }

        Assert.True(limiter.GetStatus(user.Email, null).IsLimited);

        var token = userService.CreatePasswordResetToken(user.UserId);
        Assert.True(userService.TryResetPassword(token, "Reset-After-Rate-Limit-2026!"));

        Assert.False(limiter.GetStatus(user.Email, null).IsLimited);
    }

    private HttpClient CreateClient()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
        return client;
    }

    private async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/Login", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var match = Regex.Match(
            html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase);

        Assert.True(match.Success, "Login GET must render an antiforgery request token.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static FormUrlEncodedContent CreateForm(params (string Key, string Value)[] values) =>
        new(values.ToDictionary(item => item.Key, item => item.Value));

    private static string GetAccountKeyHash(string email)
    {
        const string testPepper = "dGVzdC1rZXktcGVwcGVyLWZvci1pbnRlZ3JhdGlvbi10ZXN0cy0yMDI2LTMyLWJ5dGVz";
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var input = Encoding.UTF8.GetBytes("account\n" + normalizedEmail);
        return Convert.ToHexString(
            HMACSHA256.HashData(Convert.FromBase64String(testPepper), input));
    }

    private async Task<Users> SeedUserAsync() =>
        (await SeedUsersAsync(1))[0];

    private async Task<List<Users>> SeedUsersAsync(int count)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<UserPasswordHasher>();
        var users = new List<Users>(count);

        for (var index = 0; index < count; index++)
        {
            var id = Guid.NewGuid().ToString("N");
            var user = new Users
            {
                UserName = $"login-limit-{id}",
                Email = $"login-limit-{id}@example.test",
                Password = "pending",
                UserImage = "Defult.jpg",
                ActiveCode = Guid.NewGuid().ToString("N"),
                RegisterDate = DateTime.UtcNow,
                IsActive = true,
                IsDelete = false
            };

            user.Password = hasher.HashPassword(user, "Initial-Password-2026!");
            users.Add(user);
        }

        db.Users.AddRange(users);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return users;
    }
}
