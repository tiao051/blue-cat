using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using MongoDB.Driver;

namespace DailyTracker.Api.Migrations;

/// <summary>6 collections (spec §6) + unique indexes. M0 checklist item 2.</summary>
public sealed class M0001_CollectionsAndIndexes : IMigration
{
    public string Id => "M0001_CollectionsAndIndexes";

    public async Task UpAsync(IMongoDatabase db, CancellationToken ct)
    {
        var existing = (await db.ListCollectionNames().ToListAsync(ct)).ToHashSet();
        foreach (var name in CollectionNames.All.Where(n => !existing.Contains(n)))
            await db.CreateCollectionAsync(name, cancellationToken: ct);

        // daily_entries: the date is the unique key
        await db.GetCollection<DailyEntry>(CollectionNames.DailyEntries).Indexes.CreateOneAsync(
            new CreateIndexModel<DailyEntry>(
                Builders<DailyEntry>.IndexKeys.Ascending(e => e.Date),
                new CreateIndexOptions { Unique = true, Name = "ux_date" }),
            cancellationToken: ct);

        await db.GetCollection<MetricDefinition>(CollectionNames.MetricDefinitions).Indexes.CreateOneAsync(
            new CreateIndexModel<MetricDefinition>(
                Builders<MetricDefinition>.IndexKeys.Ascending(d => d.Key),
                new CreateIndexOptions { Unique = true, Name = "ux_key" }),
            cancellationToken: ct);

        await db.GetCollection<Habit>(CollectionNames.Habits).Indexes.CreateOneAsync(
            new CreateIndexModel<Habit>(
                Builders<Habit>.IndexKeys.Ascending(h => h.Key),
                new CreateIndexOptions { Unique = true, Name = "ux_key" }),
            cancellationToken: ct);

        var tasks = db.GetCollection<TaskItem>(CollectionNames.Tasks);
        await tasks.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<TaskItem>(
                    Builders<TaskItem>.IndexKeys.Ascending(t => t.PlannedDate),
                    new CreateIndexOptions { Name = "ix_plannedDate" }),
                new CreateIndexModel<TaskItem>(
                    Builders<TaskItem>.IndexKeys.Ascending(t => t.ScopeKey),
                    new CreateIndexOptions { Name = "ix_scopeKey" }),
            ],
            cancellationToken: ct);

        await db.GetCollection<HabitTarget>(CollectionNames.HabitTargets).Indexes.CreateOneAsync(
            new CreateIndexModel<HabitTarget>(
                Builders<HabitTarget>.IndexKeys.Ascending(t => t.HabitKey).Ascending(t => t.EffectiveFrom),
                new CreateIndexOptions { Name = "ix_habitKey_effectiveFrom" }),
            cancellationToken: ct);
    }
}
