using VSpark.Models.Auth;

namespace VSpark.Services.Auth;

public interface IAuthService
{
    public Task<AuthResponse> TryLoginAsync(AuthRequest request);

    public Task<AuthResponse> TryRegisterAsync(RegRequest request);

    public Task<AuthResponse> TryLogoutAsync(string refresh);

    public Task<AuthResponse> TryRenewSessionAsync(string refresh);

    public Task<AuthResponse> TryChangePasswordAsync(AuthRequest request, string refresh);
}
