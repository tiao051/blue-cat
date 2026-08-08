using DailyTracker.Api;
using DailyTracker.Api.Auth;
using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using DailyTracker.Api.GraphQL;
using DailyTracker.Api.Migrations;

// .env is local-dev only — real env vars (compose/systemd) always win
EnvFile.Load(System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<MetricValidationService>();
builder.Services.AddSingleton<DayLifecycleService>();
builder.Services.AddSingleton<TaskService>();

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

// Migrations run at startup; `dotnet run -- migrate` runs them and exits
await MigrationRunner.RunAsync(mongo.Database, logger);
if (args.Contains("migrate"))
{
    logger.LogInformation("Migrate mode: done, exiting.");
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
