using System.ComponentModel.DataAnnotations;

namespace VSpark.Models.Auth;

public class AuthRequest
{
    [Required]
    public required string Username { get; set; }

    [Required]
    public required string Password { get; set; }

    [Required]
    public required string DeviceId { get; set; }

    public string? NewPassword { get; set; }
}
