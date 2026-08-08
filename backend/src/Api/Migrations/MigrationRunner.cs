using DailyTracker.Api.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DailyTracker.Api.Migrations;

public interface IMigration
{
    /// <summary>Unique id, executed in sort order — e.g. "M0001_CollectionsAndIndexes".</summary>
    string Id { get; }

    Task UpAsync(IMongoDatabase db, CancellationToken ct);
}

/// <summary>
/// Runs migrations not yet recorded in the `migrations` collection, in Id order.
/// Idempotent on two levels: the runner skips recorded migrations, and each migration
/// itself uses upsert/create-if-missing so accidental re-runs never duplicate (M0 checklist).
/// </summary>
public static class MigrationRunner
{
    public static readonly IMigration[] All =
    [
        new M0001_CollectionsAndIndexes(),
        new M0002_SeedV1(),
    ];

    public static async Task RunAsync(IMongoDatabase db, ILogger logger, CancellationToken ct = default)
    {
        var ledger = db.GetCollection<BsonDocument>(CollectionNames.Migrations);
        var appliedIds = (await ledger.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(ct))
            .Select(d => d["_id"].AsString)
            .ToHashSet();

        foreach (var migration in All.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            if (appliedIds.Contains(migration.Id))
            {
                logger.LogDebug("Migration {Id} already applied, skipping", migration.Id);
                continue;
            }

            logger.LogInformation("Applying migration {Id}...", migration.Id);
            await migration.UpAsync(db, ct);
            await ledger.InsertOneAsync(
                new BsonDocument { { "_id", migration.Id }, { "appliedAt", DateTime.UtcNow } },
                cancellationToken: ct);
            logger.LogInformation("Migration {Id} done", migration.Id);
        }
    }
}
