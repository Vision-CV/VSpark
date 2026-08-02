using VSpark.Models.Auth;
using VSpark.Models.DTO;

namespace VSpark.Services.Auth;

public interface ITokenManager
{
    public JwtTokenDto CreateJwtToken(User owner, Guid sessionId);

    public string CreateRefreshToken();

    public SessionTokensDto CreateSessionTokensPair(User owner, Guid sessionId);

    public string CreateApiToken(string service);

    public bool VerifyApiToken(string token);
}
