using VSpark.Services.Auth;

namespace VSpark.Services.Background;

public class SessionsCleanupWorker(ISessionManager sessionManager, ILogger<SessionsCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await sessionManager.RevokeAllExpiredSessionsAsync();

            await Task.Delay(TimeSpan.FromDays(3), stoppingToken);
        }
    }
}
