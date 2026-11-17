using DataLayer.Context;
using Core.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Services;

public sealed class AuthenticationRateLimitCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<LoginRateLimitOptions> options,
    ILogger<AuthenticationRateLimitCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _retention = TimeSpan.FromHours(options.Value.RetentionHours);
    private readonly TimeSpan _accountWindow = TimeSpan.FromMinutes(options.Value.AccountWindowMinutes);
    private readonly TimeSpan _ipWindow = TimeSpan.FromMinutes(options.Value.IpWindowMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Delay the first cleanup until after startup. This avoids a database
                // connection during startup in endpoints that intentionally run DB-free.
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
                var now = DateTime.UtcNow;
                var retentionCutoff = now.Subtract(_retention);
                var accountWindowCutoff = now.Subtract(_accountWindow);
                var ipWindowCutoff = now.Subtract(_ipWindow);

                await context.AuthenticationRateLimitEntries
                    .Where(entry =>
                        // Failed-attempt-only buckets are disposable as soon as their
                        // observation window has elapsed; this bounds attacker-created keys.
                        (entry.ConsecutiveLockouts == 0
                            && entry.BlockedUntilUtc == null
                            && ((entry.Scope == 1 && entry.LastAttemptUtc < accountWindowCutoff)
                                || (entry.Scope == 2 && entry.LastAttemptUtc < ipWindowCutoff)))
                        // Keep escalation history for a bounded period, but never remove
                        // an active cooldown before it expires.
                        || (entry.LastAttemptUtc < retentionCutoff
                            && (entry.BlockedUntilUtc == null || entry.BlockedUntilUtc <= now)))
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // The service retries on the next interval. In particular, this can
                // happen briefly when deployment migrations have not run yet.
                logger.LogWarning(
                    exception,
                    "Authentication rate-limit cleanup failed; expired entries will be retried later.");
            }

        }
    }
}
