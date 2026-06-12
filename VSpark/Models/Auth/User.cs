namespace VSpark.Models.Auth;

public class User
{
    public Guid UserId { get; set; }

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? SecondName { get; set; }

    public string? Role { get; set; }

    public string? PasswordHash { get; set; }
}
