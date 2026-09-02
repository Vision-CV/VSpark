using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.Net;

using VSpark.Models.Auth;
using VSpark.Models.Config;
using VSpark.Models.DTO;
using VSpark.Persistence;

using static BCrypt.Net.BCrypt;

namespace VSpark.Services.Auth;

// TODO: Transactions required.
public class AuthService(IOptions<AuthSettings> authSettings, IDbContextFactory<SparkDbContext> dbFactory, ISessionManager sessionManager) : IAuthService
{
    public async Task<AuthResponse> TryLoginAsync(AuthRequest request)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username);
        
        if (targetUser == null)
            return AuthResponse.Fail(HttpStatusCode.NotFound, "There's no user associated with this username found.");

        if (!Verify(request.Password, targetUser.PasswordHash))
            return AuthResponse.Fail(HttpStatusCode.Unauthorized, "Wrong password");

        return await CreateSessionAsync(targetUser);
    }

    public async Task<AuthResponse> TryLogoutAsync(string refresh)
    {
        await sessionManager.RevokeSessionAsync(refresh);

        return AuthResponse.Success("Logout successful");
    }

    public async Task<AuthResponse> TryRenewSessionAsync(string refresh)
    {
        SessionTokensDto? tokens = await sessionManager.RotateTokensAsync(refresh);

        if (tokens == null)
            return AuthResponse.Fail(HttpStatusCode.InternalServerError, "Failed to renew session. Please try again.");

        AuthResponse successResponse = AuthResponse.Success(tokens.JwtToken);
        successResponse.AppendCookies("Session-Refresh-Token", tokens.RefreshToken);

        return successResponse;
    }

    public async Task<AuthResponse> TryRegisterAsync(RegRequest request)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        if (await dbContext.Users.AnyAsync(x => x.Username == request.Username))
            return AuthResponse.Fail(HttpStatusCode.BadRequest, "User with the current username already exists.");

        string passwordHash = HashPassword(request.Password);

        User createdUser = new User()
        {
            UserId = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = passwordHash,
            FirstName = request.Name,
            SecondName = request.Surname,
            Role = authSettings.Value.DefaultRole
        };

        dbContext.Users.Add(createdUser);

        await dbContext.SaveChangesAsync();
        
        return await CreateSessionAsync(createdUser);
    }

    public async Task<AuthResponse> TryChangePasswordAsync(AuthRequest request, string refresh)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username);

        if (targetUser == null)
            return AuthResponse.Fail(HttpStatusCode.NotFound, "Looks like there's no user with the specified username found.");

        if (!Verify(request.Password, targetUser.PasswordHash))
            return AuthResponse.Fail(HttpStatusCode.Unauthorized, "Wrong password.");

        targetUser.PasswordHash = HashPassword(request.NewPassword);

        await dbContext.SaveChangesAsync();

        await sessionManager.RevokeAllUserSessionsAsync(targetUser);

        return AuthResponse.Success(message: "Password successfully changed!");
    }

    private async Task<AuthResponse> CreateSessionAsync(User user)
    {
        SessionTokensDto tokensDto = await sessionManager.CreateSessionAsync(user);

        AuthResponse sessionCreationResponse = AuthResponse.Success(tokensDto.JwtToken);
        sessionCreationResponse.AppendCookies("Session-Refresh-Token", tokensDto.RefreshToken);

        return sessionCreationResponse;
    }
}
