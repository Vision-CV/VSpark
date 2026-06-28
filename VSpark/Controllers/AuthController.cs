using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using VSpark.Data;
using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Config;
using VSpark.Services;

namespace VSpark.Controllers;

using BCrypt = BCrypt.Net.BCrypt;

[ApiController]
[Route("auth")]
public class AuthController(IOptions<JwtSettings> jwtSettings, IOptions<AuthSettings> authSettings, IDbContextFactory<SparkDbContext> dbFactory, ITokenManager tokenProvider) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest? authRequest)
    {
        if (authRequest == null)
            return BadRequest("No login data was received.");

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == authRequest.Username);

        if (targetUser == null)
            return Unauthorized();

        if (!BCrypt.Verify(authRequest.Password, targetUser.PasswordHash))
            return Unauthorized();

        DateTime refreshTokenExpires = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays);

        RefreshToken? newRefreshToken = await tokenProvider.CreateRefreshTokenAsync(targetUser, refreshTokenExpires, authRequest.DeviceId);

        if (newRefreshToken == null || newRefreshToken.Token == null)
            return StatusCode(500, "Failed to provide you a new refresh token. Please try again later or contact server administrator.");

        string? newJwtToken = tokenProvider.CreateJwtToken(targetUser);

        if (newJwtToken == null)
            return StatusCode(500, "Failed to provide you a new jwt token. Please try again later or contact server administrator.");

        Response.Cookies.Append("Session-Refresh-Token", newRefreshToken.Token, GetRefreshTokenCookieOptions());

        return Ok(new { accessToken = newJwtToken, refreshExpires = refreshTokenExpires });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegRequest? regRequest)
    {
        if (regRequest == null)
            return BadRequest("Bad request data.");

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        if (await dbContext.Users.AnyAsync(x => x.Username == regRequest.Username))
            return Conflict("User with the same username is already registered.");

        User newUser = new User { Username = regRequest.Username, FirstName = regRequest.Name, SecondName = regRequest.Surname, UserId = Guid.NewGuid(), Role = authSettings.Value.DefaultRole };
        newUser.PasswordHash = BCrypt.HashPassword(regRequest.Password);

        dbContext.Users.Add(newUser);

        await dbContext.SaveChangesAsync();

        string? jwtToken = tokenProvider.CreateJwtToken(newUser);

        if (jwtToken == null)
            return StatusCode(500, "JWT creation failed.");

        DateTime refreshTokenExpires = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays);

        RefreshToken? refreshToken = await tokenProvider.CreateRefreshTokenAsync(newUser, refreshTokenExpires, regRequest.DeviceId);

        if (refreshToken == null || refreshToken.Token == null)
            return StatusCode(500, "Refresh token creation failed.");

        Response.Cookies.Append("Session-Refresh-Token", refreshToken.Token, GetRefreshTokenCookieOptions());

        return Ok(new { accessToken = jwtToken, refreshExpires = refreshTokenExpires });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] AuthRequest? authRequest)
    {
        if (authRequest == null)
            return BadRequest("Change password request data was not received.");

        if (string.IsNullOrWhiteSpace(authRequest.NewPassword))
            return BadRequest("No new password was found in the request.");

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == authRequest.Username);

        if (targetUser == null)
            return NotFound("User was not found.");

        if (!BCrypt.Verify(authRequest.Password, targetUser.PasswordHash))
            return Unauthorized("Old password is wrong.");

        targetUser.PasswordHash = BCrypt.HashPassword(authRequest.NewPassword);

        await dbContext.SaveChangesAsync();

        await tokenProvider.CleanupRefreshTokensAsync(targetUser);

        return Ok(new { message = "Password was successfully changed." });
    }

    [HttpPost("renew-session")]
    public async Task<IActionResult> RenewSession()
    {
        if (!Request.Cookies.TryGetValue("Session-Refresh-Token", out string? refreshToken) || refreshToken == null)
            return Unauthorized("You're not authorized.");

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (targetToken == null)
            return Unauthorized("Your refresh token was not found. Please authorize first.");

        if (DateTime.UtcNow > targetToken.Expires)
        {
            await tokenProvider.DisposeRefreshTokenAsync(targetToken.Token!);

            return Unauthorized("Your refresh token expired. Please login.");
        }

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == targetToken.Owner);

        if (targetUser == null)
            return StatusCode(500, "We can't find any information about who you are. Did you register a new account? Please say that you do...");

        DateTime refreshTokenExpires = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays);

        RefreshToken? newRefreshToken = await tokenProvider.CreateRefreshTokenAsync(targetUser, refreshTokenExpires, targetToken.DeviceId);

        if (newRefreshToken == null || newRefreshToken.Token == null)
            return StatusCode(500, "Failed to provide you a new refresh token. Please try again later or contact server administrator.");

        await tokenProvider.DisposeRefreshTokenAsync(targetToken.Token!);

        string? newJwtToken = tokenProvider.CreateJwtToken(targetUser);

        if (newJwtToken == null)
            return StatusCode(500, "Failed to provide you a new jwt token. Please try again later or contact server administrator.");

        Response.Cookies.Append("Session-Refresh-Token", newRefreshToken.Token, GetRefreshTokenCookieOptions());

        return Ok(new { accessToken = newJwtToken, refreshExpires = refreshTokenExpires });
    }

    private CookieOptions GetRefreshTokenCookieOptions() => new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromDays(jwtSettings.Value.RefreshTokenExpirationDays)
    };
}
