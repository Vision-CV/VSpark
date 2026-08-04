namespace VSpark.Models.DTO;

public record JwtTokenDto(string Token, string Jti, DateTime ExpiresAt);

public record SessionTokensDto(string RefreshToken, JwtTokenDto JwtToken);