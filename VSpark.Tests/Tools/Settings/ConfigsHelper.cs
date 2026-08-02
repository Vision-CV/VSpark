using Microsoft.Extensions.Options;

using VSpark.Models.Config;

namespace VSpark.Tests.Tools.Settings;

public static class ConfigsHelper
{
    public static IOptions<AuthSettings> AuthOptions = Options.Create<AuthSettings>(new()
    {
        DefaultRole = "User"
    });

    public static IOptions<JwtSettings> JwtSettings = Options.Create(new JwtSettings
    {
        JwtTokenExpirationMinutes = 15,
        Issuer = "VSpark",
        Audience = "User",
        RefreshTokenExpirationDays = 3,
        Secret = "supersecret-greatest-test-key-123456"
    });
}
