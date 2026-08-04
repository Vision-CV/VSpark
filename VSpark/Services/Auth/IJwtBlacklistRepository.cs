namespace VSpark.Services.Auth;

public interface IJwtBlacklistRepository
{
    public Task<bool> VerifyAsync(string token);

    public Task BlacklistTokenAsync(string jti, DateTime expires);

    public Task CleanupExpiredTokensAsync();
}
