using System.Net;

namespace VSpark.Models.Auth;

public class AuthResponse
{
    private AuthResponse() { }

    public bool IsFailed { get; private set; } = false;

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string Message { get; private set; } = string.Empty;

    public Dictionary<string, string>? Cookies { get; private set; }

    public object? Body { get; private set; }

    public void AppendCookies(string key, string value)
    {
        Cookies ??= new();

        Cookies[key] = value;
    }

    public static AuthResponse Success(object? body = null, string? message = null)
    {
        AuthResponse response = new();

        if (message != null)
            response.Message = message;

        if (body != null)
            response.Body = body;

        return response;
    }

    public static AuthResponse Fail(HttpStatusCode reason, string? message = null)
    {
        AuthResponse response = new();
        response.StatusCode = reason;
        response.IsFailed = true;

        if (message != null)
            response.Message = message;

        return response;
    }
}
