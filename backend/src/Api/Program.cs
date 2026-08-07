using DailyTracker.Api;
using DailyTracker.Api.Data;
using DailyTracker.Api.Migrations;

// .env chỉ dùng local dev — env vars thật (compose/systemd) luôn thắng
EnvFile.Load(System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MongoContext>();

var app = builder.Build();

var mongo = app.Services.GetRequiredService<MongoContext>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Migration chạy lúc startup; `dotnet run -- migrate` thì chạy xong thoát luôn
await MigrationRunner.RunAsync(mongo.Database, logger);
if (args.Contains("migrate"))
{
    logger.LogInformation("Chế độ migrate: xong, thoát.");
    return;
}

app.MapGet("/health", async (CancellationToken ct) =>
{
    try
    {
        await mongo.PingAsync(ct);
        return Results.Ok(new { status = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "degraded", error = ex.Message }, statusCode: 503);
    }
});

app.Run();
