using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace VSpark.Models.Auth.Tokens;

public class BlacklistedJwtToken
{
    [Key]
    public string Token { get; private set; }

    public DateTime ValidTo { get; private set; }

    public BlacklistedJwtToken(string token)
    {
        JwtSecurityToken tokenToBlacklist = new JwtSecurityToken(token);

        Token = token;
        ValidTo = tokenToBlacklist.ValidTo;
    }
}