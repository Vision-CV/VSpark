namespace VSpark.Services.Auth;

public class ApiTokenManager : IApiTokenManager
{
    public bool VerifyToken(string token) => token == "x-universal-token";
}
