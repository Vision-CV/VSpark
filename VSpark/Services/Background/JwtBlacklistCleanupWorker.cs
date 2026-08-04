using VSpark.Services.Auth;

namespace VSpark.Services.Background;

public class JwtBlacklistCleanupWorker(IJwtBlacklistRepository jwtBlacklistRepository) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await jwtBlacklistRepository.CleanupExpiredTokensAsync();

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
