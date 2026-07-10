namespace VSpark.Services.Auth;

public interface IApiTokenManager
{
    public bool VerifyToken(string token);
}
