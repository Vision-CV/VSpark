using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Net;

using VSpark.Models.Auth;
using VSpark.Models.Config;
using VSpark.Services.Auth;

namespace VSpark.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IOptions<JwtSettings> jwtSettings, IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest? authRequest)
    {
        if (authRequest == null)
            return BadRequest("Failed to receive");

        // Почему без nullable?
        AuthResponse loginResponse = await authService.TryLoginAsync(authRequest);

        if (loginResponse.IsFailed)
            return StatusCode((int)loginResponse.StatusCode);

        if (loginResponse.Cookies == null || !loginResponse.Cookies.TryGetValue("Session-Refresh-Token", out string? refreshToken))
            return StatusCode((int)HttpStatusCode.InternalServerError, "There's a problem with creating a new session. Try again.");

        Response.Cookies.Append("Session-Refresh-Token", refreshToken, GetRefreshTokenCookieOptions());

        return Ok(loginResponse.Body);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegRequest? regRequest)
    {
        if (regRequest == null)
            return BadRequest("Bad request data.");

        AuthResponse? regResponse = await authService.TryRegisterAsync(regRequest);

        if (regResponse == null)
            return StatusCode(500, "We've failed to receive an answer from auth service. Please contact service administrator or try again.");

        if (regResponse.IsFailed)
            return StatusCode((int)regResponse.StatusCode, regResponse.Message);

        if (regResponse.Cookies == null || !regResponse.Cookies.TryGetValue("Session-Refresh-Token", out string? refreshToken))
            return StatusCode(500, "We've failed to receive your session's data. Please try again.");

        Response.Cookies.Append("Session-Refresh-Token", refreshToken, GetRefreshTokenCookieOptions());

        return Ok(regResponse.Body);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!Request.Cookies.TryGetValue("Session-Refresh-Token", out string? executorRefreshToken))
            return Unauthorized();

        if (string.IsNullOrEmpty(executorRefreshToken))
            return BadRequest("Failed to read your refresh token");

        AuthResponse logoutResponse = await authService.TryLogoutAsync(executorRefreshToken);

        if (logoutResponse.IsFailed)
            return StatusCode((int)logoutResponse.StatusCode, logoutResponse.Message);

        Response.Cookies.Delete("Session-Refresh-Token");

        return Ok("Successful logout.");
    }

    [HttpPatch("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] AuthRequest? authRequest)
    {
        if (authRequest == null)
            return BadRequest("Change password request data was not received.");

        if (string.IsNullOrWhiteSpace(authRequest.NewPassword))
            return BadRequest("No new password was found in the request.");

        if (!Request.Cookies.TryGetValue("Session-Refresh-Token", out string? refreshToken))
            return Unauthorized("Failed to read your refresh token.");

        AuthResponse changeResponse = await authService.TryChangePasswordAsync(authRequest, refreshToken);

        if (changeResponse.IsFailed)
            return StatusCode((int)changeResponse.StatusCode, changeResponse.Message);

        return Ok(changeResponse.Message);
    }

    [HttpPost("renew-session")]
    public async Task<IActionResult> RenewSession()
    {
        if (!Request.Cookies.TryGetValue("Session-Refresh-Token", out string? refreshToken) || refreshToken == null)
            return Unauthorized("You're not authorized.");

        AuthResponse renewResponse = await authService.TryRenewSessionAsync(refreshToken);

        if (renewResponse.IsFailed)
            return StatusCode((int)renewResponse.StatusCode, renewResponse.Message);

        if (renewResponse.Cookies == null || !renewResponse.Cookies.TryGetValue("Session-Refresh-Token", out string? newRefresh))
            return StatusCode(500, "We've failed to receive your session's data. Please try again.");

        Response.Cookies.Append("Session-Refresh-Token", newRefresh, GetRefreshTokenCookieOptions());

        return Ok(renewResponse.Body);
    }

    private CookieOptions GetRefreshTokenCookieOptions() => new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromDays(jwtSettings.Value.RefreshTokenExpirationDays)
    };
}
