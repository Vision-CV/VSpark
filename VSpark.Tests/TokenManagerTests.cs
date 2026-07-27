using System.IdentityModel.Tokens.Jwt;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Persistence;
using VSpark.Services.Auth;
using VSpark.Tests.Tools.Persistence;
using VSpark.Tests.Tools.Settings;
using VSpark.Tests.Tools.Utils;

namespace VSpark.Tests;

public class TokenManagerTests
{
    // Attention! Instances of this field have a per-test lifecycle. Do not touch em.
    private MemDbContextFactory _dbFactory;
    private SparkDbContext _dbContext;
    private TokenManager _tokenManager;

    [SetUp]
    public void Setup()
    {
        _dbFactory = new MemDbContextFactory(Guid.NewGuid().ToString());
        _dbContext = _dbFactory.CreateDbContext();

        _tokenManager = new TokenManager(ConfigsHelper.JwtSettings, _dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _dbFactory.Dispose();
        _dbContext.Dispose();
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public void JwtGenerationTest(string name, string surname, string username)
    {
        User user = UserUtils.FromStrings(name, surname, username);

        string? token = _tokenManager.CreateJwtToken(user);

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
            Assert.That(jwtToken.Issuer, Is.EqualTo(ConfigsHelper.JwtSettings.Value.Issuer));
            Assert.That(jwtToken.Audiences.First(), Is.EqualTo(ConfigsHelper.JwtSettings.Value.Audience));
            Assert.That(tokenLifetimeMinutes, Is.EqualTo(ConfigsHelper.JwtSettings.Value.AccessTokenExpirationMinutes));
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
        User targetUser = UserUtils.FromStrings(name, surname, username);

        RefreshToken? token = await _tokenManager.CreateRefreshTokenAsync(targetUser);

        Assert.That(token, Is.Not.Null, "Method returned null instead of token.");

        Assert.Multiple(() => CheckRefreshTokenIntegrity(token, targetUser));
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public async Task RefreshCreationTest_SavedToDatabaseCorrectly(string name, string surname, string username)
    {
        User targetUser = UserUtils.FromStrings(name, surname, username);

        RefreshToken? token = await _tokenManager.CreateRefreshTokenAsync(targetUser);

        Assert.That(token, Is.Not.Null, "Method returned null instead of token.");

        RefreshToken? dbToken = _dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == token!.SessionId);

        Assert.That(dbToken, Is.Not.Null, "Failed to get created token back from the database.");

        Assert.Multiple(() => CheckRefreshTokenIntegrity(dbToken, targetUser));
    }

    [TestCase("Michael", "Anderson", "mikeuser")]
    [TestCase("Sarah", "Mitchell", "sarahdev")]
    [TestCase("Daniel", "Thompson", "danieladmin")]
    [TestCase("Emma", "Wilson", "emmaoperator")]
    [TestCase("Robert", "Johnson", "robservice")]
    [TestCase("Olivia", "Brown", "oliviauser")]
    public async Task TryRevokeTokenAsync_RemovesTokenFromDatabase(string name, string surname, string username)
    {
        RefreshToken? targetToken = await _tokenManager.CreateRefreshTokenAsync(UserUtils.FromStrings(name, surname, username));

        Assert.That(targetToken, Is.Not.Null, "Refresh token is null. (are previous tests are well done?...)");

        await _tokenManager.TryRevokeRefreshTokenAsync(targetToken!.Token);

        if (_dbContext.RefreshTokens.Any(x => x.SessionId == targetToken.SessionId))
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
        User targetUser = UserUtils.FromStrings(name, surname, username);

        RefreshToken? targetToken = await _tokenManager.CreateRefreshTokenAsync(targetUser);
        RefreshToken? targetToken2 = await _tokenManager.CreateRefreshTokenAsync(targetUser);
        RefreshToken? targetToken3 = await _tokenManager.CreateRefreshTokenAsync(targetUser);

        Assert.That(targetToken, Is.Not.Null, "First token is null");
        Assert.That(targetToken2, Is.Not.Null, "Second token is null");
        Assert.That(targetToken3, Is.Not.Null, "Third token is null");

        await _tokenManager.CleanupRefreshTokensAsync(targetUser);

        RefreshToken? targetDbToken = _dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == targetToken.SessionId);
        RefreshToken? targetDbToken2 = _dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == targetToken2.SessionId);
        RefreshToken? targetDbToken3 = _dbContext.RefreshTokens.FirstOrDefault(x => x.SessionId == targetToken3.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(targetDbToken, Is.Null, "First token was not removed from the database.");
            Assert.That(targetDbToken2, Is.Null, "Second token was not removed from the database.");
            Assert.That(targetDbToken3, Is.Null, "Third token was not removed from the database.");
        });
    }

    private void CheckRefreshTokenIntegrity(RefreshToken token, User owner)
    {
        TimeSpan expiresSpan = token!.Expires - DateTime.UtcNow;

        Assert.That(token.Owner, Is.EqualTo(owner.UserId));
        Assert.That(token.Issuer, Is.EqualTo(ConfigsHelper.JwtSettings.Value.Issuer));
        Assert.That(token.Audience, Is.EqualTo(ConfigsHelper.JwtSettings.Value.Audience));
        Assert.That(expiresSpan, Is.GreaterThan(TimeSpan.FromDays(0)));
        Assert.That((int)Math.Round(expiresSpan.TotalDays), Is.EqualTo(ConfigsHelper.JwtSettings.Value.RefreshTokenExpirationDays));
    }
}
