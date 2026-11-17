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
    ILogger<AuthenticationRateLimitCleanupService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private readonly LoginRateLimitOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Delay the first cleanup until after startup. This avoids a database
                // connection during startup in endpoints that intentionally run DB-free.
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await CleanupExpiredEntriesAsync(stoppingToken);
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

    public async Task<int> CleanupExpiredEntriesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AsanwebContext>();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var retentionCutoff = now.Subtract(TimeSpan.FromHours(_options.RetentionHours));
        var accountWindowCutoff = now.Subtract(TimeSpan.FromMinutes(_options.AccountWindowMinutes));
        var ipWindowCutoff = now.Subtract(TimeSpan.FromMinutes(_options.IpWindowMinutes));
        var totalDeleted = 0;

        // Every DELETE is its own short statement/transaction. TOP bounds the rows
        // affected per statement, while MaxBatchesPerRun bounds total cleanup work.
        for (var batch = 0; batch < _options.CleanupMaxBatchesPerRun; batch++)
        {
            var deleted = await context.Database.ExecuteSqlAsync($"""
                DELETE TOP ({_options.CleanupBatchSize})
                FROM [dbo].[AuthenticationRateLimitEntries]
                WHERE
                    (
                        [ConsecutiveLockouts] = 0
                        AND [BlockedUntilUtc] IS NULL
                        AND
                        (
                            ([Scope] = 1 AND [LastAttemptUtc] < {accountWindowCutoff})
                            OR ([Scope] = 2 AND [LastAttemptUtc] < {ipWindowCutoff})
                        )
                    )
                    OR
                    (
                        [LastAttemptUtc] < {retentionCutoff}
                        AND ([BlockedUntilUtc] IS NULL OR [BlockedUntilUtc] <= {now})
                    )
                """, cancellationToken);

            totalDeleted += deleted;

            if (deleted < _options.CleanupBatchSize)
            {
                break;
            }
        }

        return totalDeleted;
    }
}
