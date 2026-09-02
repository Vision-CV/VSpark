using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using VSpark.Models.Auth;
using VSpark.Models.DTO;
using VSpark.Services.Auth;
using VSpark.Tests.Tools.Settings;

namespace VSpark.Tests.Orchestrator;

public class TokenManagerTests
{
    private int _refreshGenerationCheckIterations = 10000;

    private TokenManager _tokenManager;

    [SetUp]
    public void Setup() => _tokenManager = new TokenManager(ConfigsHelper.JwtSettings);

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User", "7f8b4f4e-4b7e-4d5e-9c3e-1f7d8b9a1234")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin", "b1a2c3d4-e5f6-4789-8123-abcdef123456")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator", "11111111-2222-3333-4444-555555555555")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User", "00000000-0000-0000-0000-000000000002")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer", "66666666-7777-8888-9999-aaaaaaaaaaaa")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support", "fedcba98-7654-3210-fedc-ba9876543210")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN", "13572468-2468-1357-2468-135724681357")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System", "99999999-8888-7777-6666-555555555555")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service", "12341234-5678-90ab-cdef-1234567890ab")]
    public void CreateJwtToken_CreatedSuccessfully(string userId, string username, string role, string sessionId)
    {
        User owner = BuildUser(userId, username, role);

        JwtTokenDto jwtToken = _tokenManager.CreateJwtToken(owner, Guid.Parse(sessionId));

        int jwtLifetimeMinutes = (int)Math.Round((jwtToken.ExpiresAt - DateTime.UtcNow).TotalMinutes);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(jwtToken.Token, Is.Not.Null.Or.Empty);
            Assert.That(jwtLifetimeMinutes, Is.EqualTo(ConfigsHelper.JwtSettings.Value.JwtTokenExpirationMinutes));
        }

        JwtSecurityToken jwtTokenObj = new JwtSecurityToken(jwtToken.Token);

        Dictionary<string, string> jwtTokenClaims = ClaimsToDict(jwtTokenObj.Claims);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(jwtTokenClaims[JwtRegisteredClaimNames.Sub], Is.EqualTo(userId));
            Assert.That(jwtTokenClaims[JwtRegisteredClaimNames.Name], Is.EqualTo(username));
            Assert.That(jwtTokenClaims[JwtRegisteredClaimNames.Jti], Is.EqualTo(jwtToken.Jti));
            Assert.That(jwtTokenClaims[JwtRegisteredClaimNames.Sid], Is.EqualTo(sessionId));
            Assert.That(jwtTokenClaims[ClaimTypes.Role], Is.EqualTo(role));
        }
    }

    [Test]
    public void CreateRefreshToken_CreatedSuccessfully()
    {
        double averageLength = 0;

        HashSet<string> refreshTokens = new();
        for (int i = 0; i < _refreshGenerationCheckIterations; i++)
        {
            string newRefresh = _tokenManager.CreateRefreshToken();

            if (refreshTokens.Contains(newRefresh))
                Assert.Fail("Refresh tokens uniqueness breach detected.");

            refreshTokens.Add(newRefresh);

            averageLength += newRefresh.Length;
        }

        averageLength = averageLength / _refreshGenerationCheckIterations;

        Assert.That(averageLength, Is.EqualTo(44.0));
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", "john.doe", "User", "7f8b4f4e-4b7e-4d5e-9c3e-1f7d8b9a1234")]
    [TestCase("8b7e4c2d-2a61-4b91-9f5e-1a2c3d4e5f60", "admin", "Admin", "b1a2c3d4-e5f6-4789-8123-abcdef123456")]
    [TestCase("12345678-1234-4321-8765-123456789abc", "guest", "Guest", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    [TestCase("deadbeef-dead-beef-dead-beefdeadbeef", "moderator.user", "Moderator", "11111111-2222-3333-4444-555555555555")]
    [TestCase("00000000-0000-0000-0000-000000000001", "a", "User", "00000000-0000-0000-0000-000000000002")]
    [TestCase("11111111-2222-3333-4444-555555555555", "very.long.username.with.many.characters.testing.jwt.claims", "Developer", "66666666-7777-8888-9999-aaaaaaaaaaaa")]
    [TestCase("abcdefab-cdef-abcd-efab-cdefabcdefab", "user_123456789", "Support", "fedcba98-7654-3210-fedc-ba9876543210")]
    [TestCase("01234567-89ab-cdef-0123-456789abcdef", "UPPERCASE.USER", "ADMIN", "13572468-2468-1357-2468-135724681357")]
    [TestCase("99999999-9999-9999-9999-999999999999", "user-with-special_chars.123", "System", "99999999-8888-7777-6666-555555555555")]
    [TestCase("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0", "Service", "12341234-5678-90ab-cdef-1234567890ab")]
    public void CreateSessionTokensPair_CreatedSuccessfully(string userId, string username, string role, string sessionId)
    {
        User owner = BuildUser(userId, username, role);

        SessionTokensDto tokensDto = _tokenManager.CreateSessionTokensPair(owner, Guid.Parse(sessionId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tokensDto.JwtToken, Is.Not.Null.Or.Empty);
            Assert.That(tokensDto.RefreshToken, Is.Not.Null.Or.Empty);
        }
    }

    private User BuildUser(string userId, string username, string role) => new User
    {
        UserId = Guid.Parse(userId),
        Username = username,
        Role = role
    };

    private Dictionary<string, string> ClaimsToDict(IEnumerable<Claim> source)
    {
        Dictionary<string, string> output = new Dictionary<string, string>();

        foreach (Claim claim in source)
            output.Add(claim.Type, claim.Value);

        return output;
    }
}
