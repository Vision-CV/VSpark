using Microsoft.EntityFrameworkCore;

using VSpark.Models.Auth.Tokens;
using VSpark.Persistence;

namespace VSpark.Services.Auth;

public class JwtBlacklistRepository(IDbContextFactory<SparkDbContext> dbFactory) : IJwtBlacklistRepository
{
    public async Task AddToBlacklistAsync(string token)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        if (dbContext.JwtBlacklist.Any(x => x.Token == token))
            return;

        BlacklistedJwtToken blacklistedToken = new BlacklistedJwtToken(token);

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
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        if (await dbContext.JwtBlacklist.AnyAsync(x => x.Token == token))
            return false;

        return true;
    }
}
