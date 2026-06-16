using System.ComponentModel.DataAnnotations;

namespace VSpark.Models.Auth;

public class AuthRequest
{
    [Required]
    public string? Username { get; set; }

    [Required]
    public string? Password { get; set; }

    public string? NewPassword { get; set; }
}
