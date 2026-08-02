using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

using VSpark.Models.DTO;

namespace VSpark.Models.Auth.Sessions;

public class AuthSession
{
    private AuthSession() { }

    public AuthSession(User owner, DateTime expires, SessionTokensDto tokens)
    {
        SessionId = Guid.NewGuid();

        OwnerId = owner.UserId;
        ExpiresAt = expires;

        RefreshTokenHash = HashRefreshToken(tokens.RefreshToken);

        JwtId = tokens.JwtToken.Jti;
        JwtExpires = tokens.JwtToken.ExpiresAt;
    }

    [Key]
    public Guid SessionId { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public Guid OwnerId { get; private set; }

    public string RefreshTokenHash { get; private set; } = string.Empty;

    public string JwtId { get; private set; }

    public DateTime JwtExpires { get; private set; }

    public void SetTokens(SessionTokensDto tokens)
    {
        RefreshTokenHash = HashRefreshToken(tokens.RefreshToken);

        JwtId = tokens.JwtToken.Jti;
        JwtExpires = tokens.JwtToken.ExpiresAt;
    }

    public bool OwnsRefresh(string refresh) => RefreshTokenHash == HashRefreshToken(refresh);

    public static string HashRefreshToken(string refresh)
    {
        byte[] refreshBytes = Encoding.UTF8.GetBytes(refresh);
        byte[] refreshSha256 = SHA256.HashData(refreshBytes);

        return Convert.ToBase64String(refreshSha256);
    }
}
