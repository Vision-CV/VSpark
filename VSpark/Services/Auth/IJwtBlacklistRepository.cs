namespace VSpark.Services.Auth;

public interface IJwtBlacklistRepository
{
    public Task<bool> VerifyAsync(string token);

    public Task AddToBlacklistAsync(string token);

    public Task CleanupExpiredTokensAsync();
}
