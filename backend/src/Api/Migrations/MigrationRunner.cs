using DailyTracker.Api.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DailyTracker.Api.Migrations;

public interface IMigration
{
    /// <summary>Định danh duy nhất, chạy theo thứ tự sort — vd "M0001_CollectionsAndIndexes".</summary>
    string Id { get; }

    Task UpAsync(IMongoDatabase db, CancellationToken ct);
}

/// <summary>
/// Chạy các migration chưa có trong collection `migrations`, theo thứ tự Id.
/// Idempotent 2 tầng: runner bỏ qua migration đã ghi sổ, và bản thân mỗi migration
/// dùng upsert/create-if-missing nên lỡ chạy lại cũng không nhân đôi (checklist M0).
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
                logger.LogDebug("Migration {Id} đã chạy, bỏ qua", migration.Id);
                continue;
            }

            logger.LogInformation("Chạy migration {Id}...", migration.Id);
            await migration.UpAsync(db, ct);
            await ledger.InsertOneAsync(
                new BsonDocument { { "_id", migration.Id }, { "appliedAt", DateTime.UtcNow } },
                cancellationToken: ct);
            logger.LogInformation("Migration {Id} xong", migration.Id);
        }
    }
}
