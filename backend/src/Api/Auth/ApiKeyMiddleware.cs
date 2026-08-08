using System.Security.Cryptography;
using System.Text;

namespace DailyTracker.Api.Auth;

/// <summary>
/// Auth for a single-user app (spec §10): compares X-Secret-Key against APP_SECRET_KEY.
/// Enforced only on POST /graphql (where the data lives) — /health and Nitro IDE assets stay open.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly byte[] _secret = Encoding.UTF8.GetBytes(config["APP_SECRET_KEY"] ?? "");

    public async Task InvokeAsync(HttpContext context)
    {
        var isGraphQlPost = context.Request.Path.StartsWithSegments("/graphql")
                            && HttpMethods.IsPost(context.Request.Method);

        if (isGraphQlPost && !IsAuthorized(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
            return;
        }

        await next(context);
    }

    private bool IsAuthorized(HttpContext context)
    {
        if (_secret.Length == 0) return false; // no secret configured = locked door, not an open one

        if (!context.Request.Headers.TryGetValue("X-Secret-Key", out var provided)) return false;
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        return CryptographicOperations.FixedTimeEquals(providedBytes, _secret);
    }
}
