using Microsoft.Extensions.Options;

using System.IdentityModel.Tokens.Jwt;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Sessions;
using VSpark.Models.Config;
using VSpark.Models.DTO;
using VSpark.Persistence;
using VSpark.Services.Auth;
using VSpark.Tests.Tools.Persistence;
using VSpark.Tests.Tools.Settings;

namespace VSpark.Tests;

public class SessionManagerTests
{
    private ISessionManager _sessionManager;
    private ITokenManager _tokenManager;
    private IJwtBlacklistRepository _jwtBlacklist;

    private IOptions<AuthSettings> _authSettings;
    private IOptions<JwtSettings> _jwtSettings;

    private MemDbContextFactory _dbFactory;
    private SparkDbContext _dbContext;

    [SetUp]
    public void Setup()
    {
        _authSettings = ConfigsHelper.AuthOptions;
        _jwtSettings = ConfigsHelper.JwtSettings;

        _dbFactory = new MemDbContextFactory(Guid.NewGuid().ToString());
        _dbContext = _dbFactory.CreateDbContext();

        _tokenManager = new TokenManager(_jwtSettings);
        _jwtBlacklist = new JwtBlacklistRepository(_dbFactory);

        _sessionManager = new SessionManager(_authSettings, _dbFactory, _tokenManager, _jwtBlacklist);
    }

    [TearDown]
    public void TearDown()
    {
        _dbFactory.Dispose();
        _dbContext.Dispose();
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service")]
    public async Task CreateSessionAsync_CreatedAndSavedIntoDb_SessionDataVerified(string userId, string username, string role)
    {
        User user = BuildUser(userId, username, role);

        SessionTokensDto tokens = await _sessionManager.CreateSessionAsync(user);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tokens.JwtToken, Is.Not.Null);
            Assert.That(tokens.JwtToken.Token, Is.Not.Null.Or.Empty);
            Assert.That(tokens.RefreshToken, Is.Not.Null.Or.Empty);
        }

        JwtSecurityToken jwtToken = new JwtSecurityToken(tokens.JwtToken.Token);

        AuthSession? createdSession = _dbContext.Sessions.FirstOrDefault(x => x.OwnerId == user.UserId);

        Assert.That(createdSession, Is.Not.Null);
        Assert.That(createdSession.RefreshTokenHash, Is.Not.Null.Or.Empty);
        Assert.That(createdSession.JwtId, Is.Not.Null.Or.Empty);

        Assert.That(jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sid).Value, Is.EqualTo(createdSession!.SessionId.ToString()));

        int sessionLifetimeDays = (int)Math.Round((createdSession.ExpiresAt - DateTime.UtcNow).TotalDays);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(createdSession.RefreshTokenHash, Is.Not.Null.Or.Empty);
            Assert.That(createdSession.RefreshTokenHash, Is.Not.EqualTo(tokens.RefreshToken));
            Assert.That(createdSession.OwnsRefresh(tokens.RefreshToken), Is.True);

            Assert.That(sessionLifetimeDays, Is.EqualTo(_authSettings.Value.SessionExpirationDays));
        }
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service")]
    public async Task RotateTokensAsync_DatabaseInstanceUpdated_RotatesSecretsAndBlacklistsOldJwt(string userId, string username, string role)
    {
        User user = BuildUser(userId, username, role);

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        SessionTokensDto tokens = await _sessionManager.CreateSessionAsync(user);

        SessionTokensDto? newTokens = await _sessionManager.RotateTokensAsync(tokens.RefreshToken);

        Assert.That(newTokens, Is.Not.Null);
        Assert.That(newTokens.RefreshToken, Is.Not.Null.Or.Empty);
        Assert.That(newTokens.JwtToken, Is.Not.Null);
        Assert.That(newTokens.JwtToken.Token, Is.Not.Null.Or.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(newTokens.RefreshToken, Is.Not.EqualTo(tokens.RefreshToken));
            Assert.That(newTokens.JwtToken.Jti, Is.Not.EqualTo(tokens.JwtToken.Jti));
            Assert.That(newTokens.JwtToken.Token, Is.Not.EqualTo(tokens.JwtToken.Token));
        }

        AuthSession? session = _dbContext.Sessions.FirstOrDefault(x => x.OwnerId == user.UserId);

        Assert.That(session, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.JwtId, Is.EqualTo(newTokens.JwtToken.Jti));
            Assert.That(session.OwnsRefresh(newTokens.RefreshToken), Is.True);

            bool jwtVerified = await _jwtBlacklist.VerifyAsync(tokens.JwtToken.Token);

            Assert.That(jwtVerified, Is.False);
            Assert.That(session.OwnsRefresh(tokens.RefreshToken), Is.False);
        }
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service")]
    public async Task RevokeSessionAsync_DatabaseInstanceDeleted_JwtBlacklisted(string userId, string username, string role)
    {
        User user = BuildUser(userId, username, role);

        SessionTokensDto tokens = await _sessionManager.CreateSessionAsync(user);

        await _sessionManager.RevokeSessionAsync(tokens.RefreshToken);

        Assert.That(_dbContext.Sessions.Any(x => x.OwnerId == user.UserId), Is.False);

        bool jwtVerified = await _jwtBlacklist.VerifyAsync(tokens.JwtToken.Token);

        Assert.That(jwtVerified, Is.False);
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service")]
    public async Task RevokeAllUserSessionsAsync_AllDbInstancesDeleted_AllJwtsBlacklisted(string userId, string username, string role)
    {
        User user = BuildUser(userId, username, role);

        SessionTokensDto tokens = await _sessionManager.CreateSessionAsync(user);
        SessionTokensDto tokens2 = await _sessionManager.CreateSessionAsync(user);
        SessionTokensDto tokens3 = await _sessionManager.CreateSessionAsync(user);

        await _sessionManager.RevokeAllUserSessionsAsync(user);

        Assert.That(_dbContext.Sessions.Any(x => x.OwnerId == user.UserId), Is.False);

        bool jwtVerified = await _jwtBlacklist.VerifyAsync(tokens.JwtToken.Token);
        bool jwtVerified2 = await _jwtBlacklist.VerifyAsync(tokens2.JwtToken.Token);
        bool jwtVerified3 = await _jwtBlacklist.VerifyAsync(tokens3.JwtToken.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(jwtVerified, Is.False);
            Assert.That(jwtVerified2, Is.False);
            Assert.That(jwtVerified3, Is.False);
        }
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service")]
    public async Task RevokeAllExpiredSessionsAsync_AllDbInstancesDeleted_AllJwtsBlacklisted(string userId, string username, string role)
    {
        User user = BuildUser(userId, username, role);

        int previousDefault = ConfigsHelper.AuthOptions.Value.SessionExpirationDays;
        ConfigsHelper.AuthOptions.Value.SessionExpirationDays = -1;

        SessionTokensDto tokens;
        SessionTokensDto tokens2;
        SessionTokensDto tokens3;

        try
        {
            tokens = await _sessionManager.CreateSessionAsync(user);
            tokens2 = await _sessionManager.CreateSessionAsync(user);
            tokens3 = await _sessionManager.CreateSessionAsync(user);
        }
        finally
        {
            ConfigsHelper.AuthOptions.Value.SessionExpirationDays = previousDefault;
        }

        SessionTokensDto tokens4 = await _sessionManager.CreateSessionAsync(user);

        AuthSession? authSession = _dbContext.Sessions.FirstOrDefault(x => x.JwtId == tokens.JwtToken.Jti);
        AuthSession? authSession2 = _dbContext.Sessions.FirstOrDefault(x => x.JwtId == tokens2.JwtToken.Jti);
        AuthSession? authSession3 = _dbContext.Sessions.FirstOrDefault(x => x.JwtId == tokens3.JwtToken.Jti);
        AuthSession? authSession4 = _dbContext.Sessions.FirstOrDefault(x => x.JwtId == tokens4.JwtToken.Jti);

        await _sessionManager.RevokeAllExpiredSessionsAsync();

        Assert.That(_dbContext.Sessions.Count(x => x.OwnerId == user.UserId), Is.EqualTo(1));

        bool jwtVerified = await _jwtBlacklist.VerifyAsync(tokens.JwtToken.Token);
        bool jwtVerified2 = await _jwtBlacklist.VerifyAsync(tokens2.JwtToken.Token);
        bool jwtVerified3 = await _jwtBlacklist.VerifyAsync(tokens3.JwtToken.Token);
        bool jwtVerified4 = await _jwtBlacklist.VerifyAsync(tokens4.JwtToken.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(jwtVerified, Is.False);
            Assert.That(jwtVerified2, Is.False);
            Assert.That(jwtVerified3, Is.False);
            Assert.That(jwtVerified4, Is.True);
        }
    }

    // Create TestUser.cs with constructor like this maybe?
    private User BuildUser(string userId, string username, string role) => new User
    {
        UserId = Guid.Parse(userId),
        Username = username,
        Role = role
    };
}
