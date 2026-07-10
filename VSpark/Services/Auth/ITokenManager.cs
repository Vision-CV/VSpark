using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;

namespace VSpark.Services.Auth;

public interface ITokenManager
{
    public string? CreateJwtToken(User owner);

    public Task<RefreshToken?> CreateRefreshTokenAsync(User owner, DateTime expires);

    public Task<bool> TryRevokeRefreshTokenAsync(string token);

    public Task CleanupRefreshTokensAsync(User owner);
}
