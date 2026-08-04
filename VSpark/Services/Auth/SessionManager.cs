using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Sessions;
using VSpark.Models.Config;
using VSpark.Models.DTO;
using VSpark.Persistence;

namespace VSpark.Services.Auth;

public class SessionManager(IOptions<AuthSettings> authSettings, IDbContextFactory<SparkDbContext> dbFactory, ITokenManager tokenManager, IJwtBlacklistRepository jwtBlacklist) : ISessionManager
{
    public async Task<SessionTokensDto> CreateSessionAsync(User user)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        Guid sessionId = Guid.NewGuid();

        DateTime sessionExpires = DateTime.UtcNow.AddDays(authSettings.Value.SessionExpirationDays);

        SessionTokensDto tokens = tokenManager.CreateSessionTokensPair(user, sessionId);

        AuthSession session = new AuthSession(user, sessionExpires, tokens, sessionId);

        dbContext.Sessions.Add(session);

        await dbContext.SaveChangesAsync();

        return new SessionTokensDto(tokens.RefreshToken, tokens.JwtToken);
    }

    public async Task RevokeSessionAsync(string refresh)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        AuthSession? session = await GetSessionByRefreshAsync(refresh, dbContext);

        if (session == null)
            return;

        await RevokeSessionAsync(session, dbContext);
    }

    public async Task RevokeSessionAsync(AuthSession session, SparkDbContext dbContext)
    {
        dbContext.Sessions.Remove(session);

        await dbContext.SaveChangesAsync();

        await jwtBlacklist.BlacklistTokenAsync(session.JwtId, session.JwtExpires);
    }

    public async Task RevokeAllUserSessionsAsync(User user)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IEnumerable<AuthSession> sessionToRevoke = await dbContext.Sessions.Where(x => x.OwnerId == user.UserId).ToListAsync();

        foreach (AuthSession session in sessionToRevoke)
            await RevokeSessionAsync(session, dbContext);
    }

    public async Task<SessionTokensDto?> RotateTokensAsync(string refresh)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        AuthSession? session = await GetSessionByRefreshAsync(refresh, dbContext);

        if (session == null)
            return null;

        User? owner = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == session.OwnerId);

        if (owner == null)
            return null;

        await jwtBlacklist.BlacklistTokenAsync(session.JwtId, session.JwtExpires);

        SessionTokensDto newSessionTokens = tokenManager.CreateSessionTokensPair(owner, session.SessionId);

        session.SetTokens(newSessionTokens);

        await dbContext.SaveChangesAsync();

        return newSessionTokens;
    }

    public async Task RevokeAllExpiredSessionsAsync()
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IEnumerable<AuthSession> sessionsToEvaporate = await dbContext.Sessions.Where(x => DateTime.UtcNow > x.ExpiresAt).ToListAsync();

        foreach (AuthSession session in sessionsToEvaporate)
            await RevokeSessionAsync(session, dbContext);
    }

    private async Task<AuthSession?> GetSessionByRefreshAsync(string refresh)
    {
        await using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        string refreshHash = AuthSession.HashRefreshToken(refresh);

        AuthSession? targetSession = await dbContext.Sessions.FirstOrDefaultAsync(x => refreshHash == x.RefreshTokenHash); ;

        if (targetSession == null)
            return null;

        return targetSession;
    }

    private async Task<AuthSession?> GetSessionByRefreshAsync(string refresh, SparkDbContext dbContext)
    {
        string refreshHash = AuthSession.HashRefreshToken(refresh);

        AuthSession? targetSession = await dbContext.Sessions.FirstOrDefaultAsync(x => refreshHash == x.RefreshTokenHash); ;

        if (targetSession == null)
            return null;

        return targetSession;
    }
}
