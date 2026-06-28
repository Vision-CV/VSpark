namespace VSpark.Services;

public interface IApiTokenManager
{
    public bool VerifyToken(string token);
}
