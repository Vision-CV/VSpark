using Microsoft.EntityFrameworkCore;

using VSpark.Models.Auth.Tokens;

using VSpark.Persistence;
using VSpark.Services.Auth;

namespace VSpark.Services.Background;

public class RefreshCleanupWorker(IDbContextFactory<SparkDbContext> dbFactory, ITokenManager tokenManager, ILogger<RefreshCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync(stoppingToken);

            DateTime now = DateTime.UtcNow;

            List<RefreshToken> tokensToDestroy = await dbContext.RefreshTokens.Where(x => x.Expires < now && (now - x.Expires).TotalDays > 3).ToListAsync(stoppingToken);

            foreach (RefreshToken targetToken in tokensToDestroy)
                if (!await tokenManager.TryRevokeRefreshTokenAsync(targetToken.Token))
                    logger.LogError($"Failed to cleanup expired token with SessionId: {targetToken.SessionId}");

            await Task.Delay((int)TimeSpan.FromDays(3).TotalMilliseconds, stoppingToken);
        }
    }
}
