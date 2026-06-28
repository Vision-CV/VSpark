using System.ComponentModel.DataAnnotations;

namespace VSpark.Models.Auth;

public class RegRequest
{
    [Required]
    public required string Username { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string DeviceId { get; set; }

    [Required]
    public required string Surname { get; set; }

    [Required]
    public required string Password { get; set; }
}
