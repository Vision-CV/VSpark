using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using System.Security.Claims;
using System.Text.Encodings.Web;

using VSpark.AuthSchemes.Configs;
using VSpark.Services.Auth;

namespace VSpark.AuthSchemes;

public class ApiKeyHandler : AuthenticationHandler<ApiKeySchemeOptions>
{
    private IApiTokenManager _apiTokenManager;

    public ApiKeyHandler(IOptionsMonitor<ApiKeySchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IApiTokenManager apiTokenManager) : base(options, logger, encoder)
    {
        _apiTokenManager = apiTokenManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var value))
            return AuthenticateResult.NoResult();

        string? apiKey = value.FirstOrDefault();

        if (apiKey == null)
            return AuthenticateResult.NoResult();

        if (!_apiTokenManager.VerifyToken(apiKey))
            return AuthenticateResult.Fail("Incorrect token");

        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.Name, "SERVICE"),
            new Claim(ClaimTypes.Role, "SA") // На время тестирований.
        };

        ClaimsIdentity identity = new ClaimsIdentity(claims, "X-API");

        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        AuthenticationTicket authTicket = new AuthenticationTicket(principal, "X-API");

        return AuthenticateResult.Success(authTicket);
    }
}
