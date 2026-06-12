using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VSpark.Data;
using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Services;

namespace VSpark.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IDbContextFactory<SparkDbContext> dbFactory, IJwtTokenProvider tokenProvider) : ControllerBase
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

        if (!BCrypt.Net.BCrypt.Verify(authRequest.Password, targetUser.PasswordHash))
            return Unauthorized();

        RefreshToken newRefreshToken = tokenProvider.Revok
    }
}
