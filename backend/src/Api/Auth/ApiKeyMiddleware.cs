using System.Security.Cryptography;
using System.Text;

namespace DailyTracker.Api.Auth;

/// <summary>
/// Auth cho app một người dùng (spec §10): so X-Secret-Key với APP_SECRET_KEY.
/// Chỉ enforce trên POST /graphql (nơi có dữ liệu) — /health và asset Nitro IDE mở.
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
        if (_secret.Length == 0) return false; // không cấu hình secret = khoá cửa, không phải mở toang

        if (!context.Request.Headers.TryGetValue("X-Secret-Key", out var provided)) return false;
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        return CryptographicOperations.FixedTimeEquals(providedBytes, _secret);
    }
}
