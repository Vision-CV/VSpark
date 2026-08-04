using VSpark.Models.Auth;
using VSpark.Models.DTO;

namespace VSpark.Services.Auth;

public interface ISessionManager
{
    public Task<SessionTokensDto> CreateSessionAsync(User user);

    public Task<SessionTokensDto?> RotateTokensAsync(string refresh);

    public Task RevokeSessionAsync(string refresh);

    public Task RevokeAllUserSessionsAsync(User user);

    public Task RevokeAllExpiredSessionsAsync();
}
