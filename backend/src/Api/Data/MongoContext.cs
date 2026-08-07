using DailyTracker.Api.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace DailyTracker.Api.Data;

public sealed class MongoContext
{
    static MongoContext()
    {
        // camelCase field names, bỏ field null (giữ document ~1KB, spec §10),
        // chịu được field lạ khi đọc lại config cũ (nguyên tắc 9)
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreIfNullConvention(true),
            new IgnoreExtraElementsConvention(true),
        };
        ConventionRegistry.Register("tracker", pack, _ => true);
    }

    public MongoContext(IConfiguration config)
    {
        var uri = config["MONGO_URI"] ?? "mongodb://localhost:27018";
        var dbName = config["MONGO_DB"] ?? "tracker";
        Client = new MongoClient(uri);
        Database = Client.GetDatabase(dbName);
    }

    public MongoClient Client { get; }
    public IMongoDatabase Database { get; }

    public IMongoCollection<MetricDefinition> MetricDefinitions => Database.GetCollection<MetricDefinition>(CollectionNames.MetricDefinitions);
    public IMongoCollection<Habit> Habits => Database.GetCollection<Habit>(CollectionNames.Habits);
    public IMongoCollection<HabitTarget> HabitTargets => Database.GetCollection<HabitTarget>(CollectionNames.HabitTargets);
    public IMongoCollection<DailyEntry> DailyEntries => Database.GetCollection<DailyEntry>(CollectionNames.DailyEntries);
    public IMongoCollection<TaskItem> Tasks => Database.GetCollection<TaskItem>(CollectionNames.Tasks);
    public IMongoCollection<Goal> Goals => Database.GetCollection<Goal>(CollectionNames.Goals);

    public Task PingAsync(CancellationToken ct = default) =>
        Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
}

public static class CollectionNames
{
    public const string MetricDefinitions = "metric_definitions";
    public const string Habits = "habits";
    public const string HabitTargets = "habit_targets";
    public const string DailyEntries = "daily_entries";
    public const string Tasks = "tasks";
    public const string Goals = "goals";
    public const string Migrations = "migrations";

    public static readonly string[] All =
        [MetricDefinitions, Habits, HabitTargets, DailyEntries, Tasks, Goals];
}
