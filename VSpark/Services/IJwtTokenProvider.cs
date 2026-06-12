using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;

namespace VSpark.Services;

// Refactor: ITokenManager. Merge all token logics inside of this service.
public interface IJwtTokenProvider
{
    public string? GenerateToken(User owner);

    // Refactor: ProvideRefreshTokenAsync. Generates and saves new token for use.
    public RefreshToken GenerateRefreshToken(User owner, DateTime expires);

    public Task CleanupRefreshTokenAsync(Guid userId);
}
