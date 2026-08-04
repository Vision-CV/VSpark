using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;

using VSpark.Models.Auth.Tokens;
using VSpark.Persistence;

namespace VSpark.Services.Auth;

public class JwtBlacklistRepository(IDbContextFactory<SparkDbContext> dbFactory) : IJwtBlacklistRepository
{
    public async Task BlacklistTokenAsync(string jti, DateTime expires)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        if (dbContext.JwtBlacklist.Any(x => x.JwtId == jti))
            return;

        BlacklistedJwtToken blacklistedToken = new BlacklistedJwtToken(jti, expires);

        dbContext.JwtBlacklist.Add(blacklistedToken);

        await dbContext.SaveChangesAsync();
    }

    public async Task CleanupExpiredTokensAsync()
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IEnumerable<BlacklistedJwtToken> tokensToEvaporate = dbContext.JwtBlacklist.Where(x => x.ValidTo < DateTime.UtcNow);

        foreach (BlacklistedJwtToken token in tokensToEvaporate)
            dbContext.JwtBlacklist.Remove(token);
    }

    public async Task<bool> VerifyAsync(string token)
    {
        JwtSecurityToken targetToken = new JwtSecurityToken(token);

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        string jti = targetToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

        if (await dbContext.JwtBlacklist.AnyAsync(x => x.JwtId == jti))
            return false;

        return true;
    }
}
