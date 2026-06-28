namespace VSpark.Services.Implementation;

public class ApiTokenManager : IApiTokenManager
{
    public bool VerifyToken(string token) => token == "x-universal-token";
}
