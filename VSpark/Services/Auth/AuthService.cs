using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.Net;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Config;
using VSpark.Persistence;

using static BCrypt.Net.BCrypt;

namespace VSpark.Services.Auth;

public class AuthService(IOptions<AuthSettings> authSettings, IDbContextFactory<SparkDbContext> dbFactory, ITokenManager tokenManager, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> TryLoginAsync(AuthRequest request)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username);
        
        if (targetUser == null)
            return AuthResponse.Fail(HttpStatusCode.NotFound, "There's no user associated with this username found.");

        if (!Verify(request.Password, targetUser.PasswordHash))
            return AuthResponse.Fail(HttpStatusCode.Unauthorized, "Wrong password");

        return await CreateSessionAsync(targetUser);
    }

    public async Task<AuthResponse> TryLogoutAsync(string refresh)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetSession = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refresh);

        if (targetSession == null)
            return AuthResponse.Fail(HttpStatusCode.NotFound, "There's no sessions found with the current SessionId");

        if (!await tokenManager.TryRevokeRefreshTokenAsync(refresh))
            return AuthResponse.Fail(HttpStatusCode.InternalServerError, "Something went wrong. Please try again.");

        return AuthResponse.Success(message: $"Session {targetSession.SessionId} successfully closed.");
    }

    public async Task<AuthResponse> TryRenewSessionAsync(string refresh)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetRefresh = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refresh);

        if (targetRefresh == null)
            return AuthResponse.Fail(HttpStatusCode.Unauthorized, "You're not authorized.");

        if (targetRefresh.Expires < DateTime.UtcNow)
            return AuthResponse.Fail(HttpStatusCode.Unauthorized, "Your refresh token expired. Please login.");

        User? owner = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == targetRefresh.Owner);

        if (owner == null)
            return AuthResponse.Fail(HttpStatusCode.NotFound, "There's no users associated with the provided refresh.");

        AuthResponse newSessionResponse = await CreateSessionAsync(owner);

        if (!await tokenManager.TryRevokeRefreshTokenAsync(refresh))
            logger.LogError($"Failed to revoke refresh token for session with id: {targetRefresh.SessionId}");

        return newSessionResponse;
    }

    public async Task<AuthResponse> TryRegisterAsync(RegRequest request)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

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
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username);

        if (targetUser == null)
            return AuthResponse.Fail(HttpStatusCode.NotFound, "Looks like there's no user with the specified username found.");

        if (!Verify(request.Password, targetUser.PasswordHash))
            return AuthResponse.Fail(HttpStatusCode.Unauthorized, "Wrong password.");

        targetUser.PasswordHash = HashPassword(request.NewPassword);

        await dbContext.SaveChangesAsync();

        List<RefreshToken> tokensToRevoke = await dbContext.RefreshTokens
            .Where(x => x.Owner == targetUser.UserId && x.Token != refresh)
            .ToListAsync();

        foreach (RefreshToken refreshToken in tokensToRevoke)
            if (!await tokenManager.TryRevokeRefreshTokenAsync(refreshToken.Token))
                logger.LogError($"Failed to reset session after password change. SessionID: {refreshToken.SessionId}");

        return AuthResponse.Success(message: "Password successfully changed!");
    }

    private async Task<AuthResponse> CreateSessionAsync(User user)
    {
        string? jwtToken = tokenManager.CreateJwtToken(user);

        RefreshToken? refreshToken = await tokenManager.CreateRefreshTokenAsync(user);

        if (refreshToken == null)
            return AuthResponse.Fail(HttpStatusCode.InternalServerError, "Failed to create a new session. Please try again or contact a server administrator");

        AuthResponse successResponse = AuthResponse.Success(jwtToken, $"Successfully authorized as {user.Username}!");
        successResponse.AppendCookies("Session-Refresh-Token", refreshToken.Token);

        return successResponse;
    }
}
