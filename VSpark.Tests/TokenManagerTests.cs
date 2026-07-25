using Microsoft.Extensions.Options;

using System.IdentityModel.Tokens.Jwt;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Config;
using VSpark.Persistence;
using VSpark.Services.Auth;
using VSpark.Tests.Tools.Persistence;

namespace VSpark.Tests;

public class TokenManagerTests
{
    private IOptions<JwtSettings> jwtSettings = Options.Create(new JwtSettings
    {
        AccessTokenExpirationMinutes = 15,
        Issuer = "VSpark",
        Audience = "User",
        RefreshTokenExpirationDays = 3,
        Secret = "supersecret-greatest-test-key-123456"
    });

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public void JwtGenerationTest(string name, string surname, string username)
    {
        User user = UserByStrings(name, surname, username);

        MemDbContextFactory fact = new MemDbContextFactory(Guid.NewGuid().ToString());

        TokenManager tokenManager = new TokenManager(jwtSettings, fact);

        string? token = tokenManager.CreateJwtToken(user);

        Assert.That(token, Is.Not.Null, "Returned JWT token is null.");

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

        JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);

        string userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

        Assert.That(userId, Is.Not.Null.Or.Empty, "userId didn't found in the token object.");

        string expiresString = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Exp).Value;

        Assert.That(expiresString, Is.Not.Null, "Expires field is null.");

        byte tokenLifetimeMinutes = (byte)Math.Round((jwtToken.ValidTo - DateTime.UtcNow).TotalMinutes);

        Assert.Multiple(() =>
        {
            Assert.That(jwtToken.Issuer, Is.EqualTo(jwtSettings.Value.Issuer));
            Assert.That(jwtToken.Audiences.First(), Is.EqualTo(jwtSettings.Value.Audience));
            Assert.That(tokenLifetimeMinutes, Is.EqualTo(jwtSettings.Value.AccessTokenExpirationMinutes));
            Assert.That(user.UserId, Is.EqualTo(Guid.Parse(userId!)));
        });
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public async Task RefreshCreationTest_SuccessfullyReturnedToken(string name, string surname, string username)
    {
        User targetUser = UserByStrings(name, surname, username);

        using MemDbContextFactory fact = new(Guid.NewGuid().ToString());

        TokenManager tokenManager = new(jwtSettings, fact);

        RefreshToken? token = await tokenManager.CreateRefreshTokenAsync(targetUser);

        Assert.That(token, Is.Not.Null, "Method returned null instead of token.");

        TimeSpan expiresSpan = token!.Expires - DateTime.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(token.Owner, Is.EqualTo(targetUser.UserId));
            Assert.That(token.Issuer, Is.EqualTo(jwtSettings.Value.Issuer));
            Assert.That(token.Audience, Is.EqualTo(jwtSettings.Value.Audience));
            Assert.That(expiresSpan, Is.GreaterThan(TimeSpan.FromDays(0)));
            Assert.That((int)Math.Round(expiresSpan.TotalDays), Is.EqualTo(jwtSettings.Value.RefreshTokenExpirationDays));
        });
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public async Task RefreshCreationTest_SavedToDatabaseCorrectly(string name, string surname, string username)
    {
        User targetUser = UserByStrings(name, surname, username);

        using MemDbContextFactory fact = new(Guid.NewGuid().ToString());

        TokenManager tokenManager = new(jwtSettings, fact);

        RefreshToken? token = await tokenManager.CreateRefreshTokenAsync(targetUser);

        Assert.That(token, Is.Not.Null, "Method returned null instead of token.");

        using SparkDbContext dbContext = fact.CreateDbContext();

        RefreshToken? dbToken = dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == token!.SessionId);

        Assert.That(dbToken, Is.Not.Null, "Failed to get created token back from the database.");

        TimeSpan expiresSpan = dbToken!.Expires - DateTime.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(dbToken.Owner, Is.EqualTo(targetUser.UserId));
            Assert.That(dbToken.Issuer, Is.EqualTo(jwtSettings.Value.Issuer));
            Assert.That(dbToken.Audience, Is.EqualTo(jwtSettings.Value.Audience));
            Assert.That(expiresSpan, Is.GreaterThan(TimeSpan.FromDays(0)));
            Assert.That((int)Math.Round(expiresSpan.TotalDays), Is.EqualTo(jwtSettings.Value.RefreshTokenExpirationDays));
        });
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public async Task TryRevokeTokenAsync_RemovesTokenFromDatabase(string name, string surname, string username)
    {
        using MemDbContextFactory fact = new(Guid.NewGuid().ToString());

        TokenManager tokenManager = new(jwtSettings, fact);

        RefreshToken? targetToken = await tokenManager.CreateRefreshTokenAsync(UserByStrings(name, surname, username));

        Assert.That(targetToken, Is.Not.Null, "Refresh token is null. (are previous tests are well done?...)");

        await tokenManager.TryRevokeRefreshTokenAsync(targetToken!.Token);

        using SparkDbContext dbContext = fact.CreateDbContext();

        if (dbContext.RefreshTokens.Any(x => x.SessionId == targetToken.SessionId))
            Assert.Fail("Token was not removed from Database correctly or there's a duplicate.");
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public async Task CleanupRefreshTokensAsync_CleanupsTokens(string name, string surname, string username)
    {
        using MemDbContextFactory fact = new(Guid.NewGuid().ToString());

        User targetUser = UserByStrings(name, surname, username);

        TokenManager tokenManager = new(jwtSettings, fact);

        RefreshToken? targetToken = await tokenManager.CreateRefreshTokenAsync(targetUser);
        RefreshToken? targetToken2 = await tokenManager.CreateRefreshTokenAsync(targetUser);
        RefreshToken? targetToken3 = await tokenManager.CreateRefreshTokenAsync(targetUser);

        Assert.That(targetToken, Is.Not.Null, "First token is null");
        Assert.That(targetToken2, Is.Not.Null, "Second token is null");
        Assert.That(targetToken3, Is.Not.Null, "Third token is null");

        await tokenManager.CleanupRefreshTokensAsync(targetUser);

        using SparkDbContext dbContext = fact.CreateDbContext();

        RefreshToken? targetDbToken = dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == targetToken.SessionId);
        RefreshToken? targetDbToken2 = dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == targetToken2.SessionId);
        RefreshToken? targetDbToken3 = dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == targetToken3.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(targetDbToken, Is.Null, "First token was not removed from the database.");
            Assert.That(targetDbToken2, Is.Null, "Second token was not removed from the database.");
            Assert.That(targetDbToken3, Is.Null, "Third token was not removed from the database.");
        });
    }

    private User UserByStrings(string name, string surname, string username) => new User
    {
        FirstName = name,
        SecondName = surname,
        Username = username,
        UserId = Guid.NewGuid(),
        Role = "SA",
        PasswordHash = "RANDOM"
    };
}
