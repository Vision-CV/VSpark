using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Config;

namespace VSpark.Services.Implementation;

// Attention! Suppressed null warnings!
public class JwtTokenProvider(IOptions<JwtSettings> jwtSettings) : IJwtTokenProvider
{
    private byte[]? _jwtSecret;

    public string? GenerateToken(User owner)
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

    public RefreshToken GenerateRefreshToken(User owner, DateTime expires)
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

        return refreshToken;
    }

    public void CleanupRefreshToken(Guid userId)
    {
        
    }
}
