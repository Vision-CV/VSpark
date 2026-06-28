using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;

namespace VSpark.Services;

public interface ITokenManager
{
    public string? CreateJwtToken(User owner);

    public Task<RefreshToken?> CreateRefreshTokenAsync(User owner, DateTime expires, string deviceId);

    public Task DisposeRefreshTokenAsync(string token);

    public Task CleanupRefreshTokensAsync(User owner);
}
