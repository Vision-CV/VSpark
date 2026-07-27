using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;

namespace VSpark.Services.Auth;

// TODO: Add CleanupExpiredRefreshTokensAsync() method.
public interface ITokenManager
{
    public string? CreateJwtToken(User owner);

    public Task<RefreshToken?> CreateRefreshTokenAsync(User owner);

    public Task<bool> TryRevokeRefreshTokenAsync(string token);

    public Task CleanupRefreshTokensAsync(User owner);
}
