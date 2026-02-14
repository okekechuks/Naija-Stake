using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace NaijaStake.API.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly IConfiguration _config;

    public RefreshTokenCleanupService(IServiceProvider services, ILogger<RefreshTokenCleanupService> logger, IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _config.GetValue<int?>("RefreshTokenCleanup:IntervalSeconds") ?? 60 * 10; // 10 minutes
        var retentionDays = _config.GetValue<int?>("RefreshTokenCleanup:RetentionDays") ?? 30;
        _logger.LogInformation("RefreshTokenCleanupService starting (interval {Interval}s, retention {Retention}d)", intervalSeconds, retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NaijaStake.Infrastructure.Data.StakeItDbContext>();

                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                var toDelete = await db.RefreshTokens
                    .Where(r => (r.ExpiresAt <= DateTime.UtcNow && r.ExpiresAt <= cutoff) || (r.RevokedAt != null && r.RevokedAt <= cutoff))
                    .ToListAsync(stoppingToken);

                if (toDelete.Count > 0)
                {
                    db.RefreshTokens.RemoveRange(toDelete);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Purged {Count} expired/old refresh tokens", toDelete.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running refresh token cleanup");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}
