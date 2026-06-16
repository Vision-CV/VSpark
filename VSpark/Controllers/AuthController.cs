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
public class AuthController(IOptions<JwtSettings> jwtSettings, IDbContextFactory<SparkDbContext> dbFactory, ITokenManager tokenProvider) : ControllerBase
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

        RefreshToken? newRefreshToken = await tokenProvider.CreateRefreshTokenAsync(targetUser, DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));

        if (newRefreshToken == null)
            return StatusCode(500, "Failed to provide you a new refresh token. Please try again later or contact server administrator.");

        string? newJwtToken = tokenProvider.CreateJwtToken(targetUser);

        if (newJwtToken == null)
            return StatusCode(500, "Failed to provide you a new jwt token. Please try again later or contact server administrator.");

        CookieOptions tokenCookieOptions = new CookieOptions 
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(jwtSettings.Value.RefreshTokenExpirationDays)
        };

        Response.Cookies.Append("Session-Refresh-Token", newRefreshToken.Token!, tokenCookieOptions);

        return Ok(new { accessToken = newJwtToken });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] AuthRequest? authRequest)
    {
        if (authRequest == null)
            return BadRequest("Change password request data was not received.");

        if (authRequest.NewPassword == null)
            return BadRequest("No new password was found in the request.");

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == authRequest.Username);

        if (targetUser == null)
            return NotFound("User was not found.");

        if (!BCrypt.Verify(authRequest.Password, targetUser.PasswordHash))
            return Unauthorized("Old password is wrong.");

        targetUser.PasswordHash = BCrypt.HashPassword(authRequest.NewPassword);

        await dbContext.SaveChangesAsync();

        await tokenProvider.CleanupRefreshTokenAsync(targetUser);

        return Ok("Password was successfully changed.");
    }

    [HttpPost("renew-session")]
    public async Task<IActionResult> RenewSession()
    {
        if (!Request.Cookies.TryGetValue("Session-Refresh-Token", out string? refreshToken) || refreshToken == null)
            return Unauthorized("You're not authorized.");

        using SparkDbContext dbContext = await dbFactory.CreateDbContextAsync();

        RefreshToken? targetToken = dbContext.RefreshTokens.FirstOrDefault(x => x.Token == refreshToken);

        if (targetToken == null)
            return Unauthorized("Your refresh token was not found. Please authorize first.");

        if (DateTime.UtcNow > targetToken.Expires)
        {
            await tokenProvider.CleanupRefreshTokenAsync(targetToken.Token!);

            return Unauthorized("Your refresh token expired. Please login.");
        }

        User? targetUser = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == targetToken.Owner);

        if (targetUser == null)
            return StatusCode(500, "We can't find any information about who you are. Did you register a new account? Please say that you do...");

        RefreshToken? newRefreshToken = await tokenProvider.CreateRefreshTokenAsync(targetUser, DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));

        if (newRefreshToken == null)
            return StatusCode(500, "Failed to provide you a new refresh token. Please try again later or contact server administrator.");

        await tokenProvider.CleanupRefreshTokenAsync(targetToken.Token!);

        string? newJwtToken = tokenProvider.CreateJwtToken(targetUser);

        if (newJwtToken == null)
            return StatusCode(500, "Failed to provide you a new jwt token. Please try again later or contact server administrator.");

        CookieOptions tokenCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(jwtSettings.Value.RefreshTokenExpirationDays)
        };

        Response.Cookies.Append("Session-Refresh-Token", newRefreshToken.Token!, tokenCookieOptions);

        return Ok(newJwtToken);
    }
}
