using DailyTracker.Api;
using DailyTracker.Api.Auth;
using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using DailyTracker.Api.GraphQL;
using DailyTracker.Api.Migrations;

// .env chỉ dùng local dev — env vars thật (compose/systemd) luôn thắng
EnvFile.Load(System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<MetricValidationService>();
builder.Services.AddSingleton<DayLifecycleService>();

var allowedOrigins = (builder.Configuration["ALLOWED_ORIGINS"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddErrorFilter<TrackerErrorFilter>()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

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

app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();

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

app.MapGraphQL();

app.Run();
