using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using VSpark.Data;
using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Config;

namespace VSpark.Services.Implementation;

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

    public async Task<RefreshToken?> CreateRefreshTokenAsync(User owner, DateTime expires)
    {
        RefreshToken refreshToken = new();
        refreshToken.Owner = owner.UserId;
        refreshToken.Expires = expires;
        refreshToken.Issuer = jwtSettings.Value.Issuer;
        refreshToken.Audience = jwtSettings.Value.Audience;

        byte[] rns = new byte[32];

        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(rns, 0, 32);

        refreshToken.Token = Convert.ToBase64String(rns);

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        if (dbContext.RefreshTokens.Any(x => x.Owner == owner.UserId))
            return null;

        dbContext.RefreshTokens.Add(refreshToken);

        return refreshToken;
    }

    public async Task CleanupRefreshTokenAsync(string token)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);

        if (targetToken == null)
            return;

        dbContext.RefreshTokens.Remove(targetToken);
    }

    public async Task CleanupRefreshTokenAsync(User owner)
    {
        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Owner == owner.UserId);

        if (targetToken == null)
            return;

        dbContext.RefreshTokens.Remove(targetToken);
    }
}
