using System.ComponentModel.DataAnnotations;

namespace VSpark.Models.Auth.Tokens;

public class BlacklistedJwtToken
{
    private BlacklistedJwtToken() { }
    
    public BlacklistedJwtToken(string jti, DateTime expires)
    {
        JwtId = jti;
        ValidTo = expires;
    }

    [Key]
    public string JwtId { get; private set; }

    public DateTime ValidTo { get; private set; }
}