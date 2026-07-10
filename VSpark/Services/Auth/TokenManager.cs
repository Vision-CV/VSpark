using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Config;
using VSpark.Persistence;

namespace VSpark.Services.Auth;

public class TokenManager(IOptions<JwtSettings> jwtSettings, IDbContextFactory<SparkDbContext> dbFactory) : ITokenManager
{
    private byte[]? _jwtSecret;

    public string? CreateJwtToken(User owner)
    {
        if (_jwtSecret == null)
            _jwtSecret = Encoding.UTF8.GetBytes(jwtSettings.Value.Secret!);

        SymmetricSecurityKey signingKey = new SymmetricSecurityKey(_jwtSecret);
        SigningCredentials signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, owner.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, owner.Username!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, owner.Role!),
        };

        JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpirationMinutes),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }

    public async Task<RefreshToken?> CreateRefreshTokenAsync(User owner, DateTime tokenExpires)
    {
        RefreshToken refreshToken = new() { Expires = tokenExpires, SessionId = Guid.NewGuid() };
        refreshToken.Owner = owner.UserId;
        refreshToken.Issuer = jwtSettings.Value.Issuer;
        refreshToken.Audience = jwtSettings.Value.Audience;

        byte[] rns = new byte[32];

        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(rns, 0, 32);

        refreshToken.Token = Convert.ToBase64String(rns);

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.SessionId == refreshToken.SessionId);

        if (targetToken != null)
            dbContext.RefreshTokens.Remove(targetToken);

        await dbContext.RefreshTokens.AddAsync(refreshToken);

        await dbContext.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<bool> TryRevokeRefreshTokenAsync(string token)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);

        if (targetToken == null)
            return false;

        dbContext.RefreshTokens.Remove(targetToken);

        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task CleanupRefreshTokensAsync(User owner)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IEnumerable<RefreshToken>? targetTokens = dbContext.RefreshTokens.Where(x => x.Owner == owner.UserId);

        if (targetTokens == null)
            return;

        foreach (RefreshToken token in targetTokens)
            dbContext.RefreshTokens.Remove(token);

        await dbContext.SaveChangesAsync();
    }
}
