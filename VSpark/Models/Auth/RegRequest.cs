using System.ComponentModel.DataAnnotations;

namespace VSpark.Models.Auth;

public class RegRequest
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Surname { get; set; }

    [Required]
    public string Password { get; set; }
}
