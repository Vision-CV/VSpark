using Microsoft.Extensions.Options;

using VSpark.Models.Auth;
using VSpark.Models.Config;
using VSpark.Services.Implementation;

namespace VSpark.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        IOptions<JwtSettings> jwtSettings = Options.Create<JwtSettings>(new JwtSettings { AccessTokenExpirationMinutes = 15, RefreshTokenExpirationDays = 15, Audience = "VisUser", Issuer = "VisDash", Secret = "23oe239jd293jh0923j092j3" });

        TokenManager tokenManager = new TokenManager(jwtSettings, null);

        Console.WriteLine(tokenManager.CreateJwtToken(new User { UserId = Guid.NewGuid() }));
        
        Assert.Pass();
    }
}
