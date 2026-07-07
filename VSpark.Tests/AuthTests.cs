using Microsoft.Extensions.Options;

using System.IdentityModel.Tokens.Jwt;

using VSpark.Models.Auth;
using VSpark.Models.Config;
using VSpark.Services.Implementation;

using VSpark.Tests.Tools;

namespace VSpark.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase("Michael", "Anderson", "mikeuser", "a3c9f2e1-8d74-4b91-9d4d-2f8d6c4e91ab")]
    [TestCase("Sarah", "Mitchell", "sarahdev", "e14b7a90-6c8d-4d53-93c1-71a5b2f49e6d")]
    [TestCase("Daniel", "Thompson", "danieladmin", "5f6e9d31-2c88-4f07-bb0d-3a917d4f1c82")]
    [TestCase("Emma", "Wilson", "emmaoperator", "c8b13a5e-91f4-46d8-8d17-ef62b9c74a10")]
    [TestCase("Robert", "Johnson", "robservice", "1b7e3c42-5a91-4f68-82d3-9e6a7c4b0f55")]
    [TestCase("Olivia", "Brown", "oliviauser", "92f4a8d1-3c67-4e90-b251-6d8f7a3c9e21")]
    public void JwtGenerationTest(string name, string surname, string username, string guid)
    {
        User user = new User
        {
            FirstName = name,
            SecondName = surname,
            Username = username,
            UserId = Guid.Parse(guid),
            Role = "SA",
            PasswordHash = "RANDOM"
        };

        IOptions<JwtSettings> jwtSettings = Options.Create(new JwtSettings
        {
            AccessTokenExpirationMinutes = 15,
            Issuer = "VSpark",
            Audience = "User",
            RefreshTokenExpirationDays = 3,
            Secret = "supersecret-greatest-test-key-123456"
        });

        TokenManager tokenManager = new TokenManager(jwtSettings, DbTools.GetFactory());

        string? token = tokenManager.CreateJwtToken(user);

        if (token == null)
            Assert.Fail("Token is null");

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

        JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);

        string userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

        if (userId == null)
            Assert.Fail("UserId from token is null");

        Assert.That(user.UserId, Is.EqualTo(Guid.Parse(userId)));
    }
}
