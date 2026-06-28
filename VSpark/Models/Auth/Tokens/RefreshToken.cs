using System.ComponentModel.DataAnnotations;

namespace VSpark.Models.Auth.Tokens;

public class RefreshToken
{
    public Guid Owner { get; set; }

    [Key]
    public string Token { get; set; }

    public string? Issuer { get; set; }

    public required string DeviceId { get; set; }

    public string? Audience { get; set; }

    public required DateTime Expires { get; set; }
}
