using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Permissions;
using VSpark.Models.Config;
using VSpark.Models.DTO;

namespace VSpark.Services.Auth;

public class TokenManager(IOptions<JwtSettings> jwtSettings) : ITokenManager
{
    private byte[]? _jwtSecret;
    private byte[]? _stsSecret;

    // TODO: API token generation placeholder for next updates.
    // Critical points:
    public string CreateApiToken(string service)
    {
        if (_jwtSecret == null)
            _jwtSecret = Encoding.UTF8.GetBytes(jwtSettings.Value.Secret!);

        SymmetricSecurityKey signingKey = new SymmetricSecurityKey(_jwtSecret);
        SigningCredentials signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, service),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("permissions", ((int)UserPermissions.ServiceAdmin).ToString()), // Here
            new Claim("type", "sts")
        };

        JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: service,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30), // Here
            signingCredentials: signingCredentials // Here
        );

        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }

    public JwtTokenDto CreateJwtToken(User owner, Guid sessionId)
    {
        if (_jwtSecret == null)
            _jwtSecret = Encoding.UTF8.GetBytes(jwtSettings.Value.Secret!);

        SymmetricSecurityKey signingKey = new SymmetricSecurityKey(_jwtSecret);
        SigningCredentials signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        string jti = Guid.NewGuid().ToString();
        DateTime expires = DateTime.UtcNow.AddMinutes(jwtSettings.Value.JwtTokenExpirationMinutes);

        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, owner.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, owner.Username!),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
            new Claim(ClaimTypes.Role, owner.Role!),
        };

        JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: signingCredentials
        );

        JwtTokenDto jwtTokenDto = new JwtTokenDto(new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken), jti, expires);

        return jwtTokenDto;
    }

    public string CreateRefreshToken()
    {
        byte[] rns = new byte[32];

        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(rns, 0, 32);

        string token = Convert.ToBase64String(rns);

        return token;
    }

    public SessionTokensDto CreateSessionTokensPair(User owner, Guid sessionId)
    {
        string refreshToken = CreateRefreshToken();
        JwtTokenDto jwtTokenDto = CreateJwtToken(owner, sessionId);

        return new SessionTokensDto(refreshToken, jwtTokenDto);
    }

    // TODO: Also a placeholder.
    public bool VerifyApiToken(string token)
    {
        return true;
    }
}
