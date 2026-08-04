using Microsoft.Extensions.Primitives;

using System.Security.Claims;

using VSpark.Services.Auth;

namespace VSpark.Middleware;

public class JwtBlacklist(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IJwtBlacklistRepository jwtBlacklist)
    {
        // TODO: Review the safety of this check
        // Suspicious isn't it?
        if (context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value == "SERVICE")
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out StringValues authContent))
        {
            await next(context);
            return;
        }

        string? bearerContent = authContent.FirstOrDefault(x => x?.Contains("Bearer") == true);

        if (string.IsNullOrEmpty(bearerContent))
        {
            await next(context);
            return;
        }

        string? token = bearerContent!.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            await next(context);
            return;
        }

        if (!await jwtBlacklist.VerifyAsync(token))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "text/plain";

            await context.Response.WriteAsync("Your token is blacklisted. Please open a new session.", context.RequestAborted);

            return;
        }

        await next(context);
    }
}
