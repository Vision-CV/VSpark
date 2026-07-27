using VSpark.Models.Auth;

namespace VSpark.Tests.Tools.Utils;

internal class UserUtils
{
    public static User FromStrings(string name, string surname, string username) => new User
    {
        FirstName = name,
        SecondName = surname,
        Username = username,
        UserId = Guid.NewGuid(),
        Role = "SA",
        PasswordHash = "RANDOM"
    };
}
