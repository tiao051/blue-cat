using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using DailyTracker.Api.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DailyTracker.Api.Tests;

/// <summary>Quick tasks + live numerator/denominator (spec §6). Needs local Mongo on port 27018.</summary>
public sealed class TaskServiceTests : IAsyncLifetime
{
    private const string Uri = "mongodb://localhost:27018";
    private readonly string _dbName = $"tracker_test_{Guid.NewGuid():N}";
    private MongoContext _ctx = null!;
    private DayLifecycleService _lifecycle = null!;
    private TaskService _svc = null!;

    private const string D1 = "2026-08-07";
    private const string D2 = "2026-08-08";

    public async Task InitializeAsync()
    {
        _ctx = new MongoContext(Uri, _dbName);
        await MigrationRunner.RunAsync(_ctx.Database, NullLogger.Instance);
        _lifecycle = new DayLifecycleService(_ctx, new MetricValidationService());
        _svc = new TaskService(_ctx, _lifecycle);
    }

    public async Task DisposeAsync() => await _ctx.Client.DropDatabaseAsync(_dbName);

    private static List<MetricValue> MorningValues() =>
    [
        new() { Key = "sleep_start", Time = "23:30" },
        new() { Key = "sleep_end", Time = "07:00" },
        new() { Key = "mood_morning", Number = 7 },
    ];

    [Fact]
    public async Task Add_task_tick_done_and_live_counters()
    {
        // 2 tasks before the morning check-in → they enter the denominator
        await _svc.AddAsync("task a", D2, D2);
        var b = await _svc.AddAsync("task b", D2, D2);

        var entry = await _lifecycle.MorningCheckinAsync(D2, MorningValues(), []);
        Assert.Equal(2, entry.QuickPlanned);

        // Added after the lock → addedLater; denominator untouched
        var c = await _svc.AddAsync("task c", D2, D2);
        await _svc.SetDoneAsync(b.Id.ToString(), true, D2);
        await _svc.SetDoneAsync(c.Id.ToString(), true, D2);

        var (done, addedLater) = await _lifecycle.QuickCountersAsync(
            (await _lifecycle.GetEntryAsync(D2))!);
        Assert.Equal(2, done);       // ratio 2/2 even though one task was added later — can exceed 1, by design
        Assert.Equal(1, addedLater);
        Assert.Equal(2, (await _lifecycle.GetEntryAsync(D2))!.QuickPlanned); // denominator doesn't budge
    }

    [Fact]
    public async Task No_tasks_for_past_days_and_closed_day_tasks_are_locked()
    {
        await Assert.ThrowsAsync<TrackerException>(() => _svc.AddAsync("too late", D1, D2));

        // a D1 task, then D1 gets closed
        var t = await _svc.AddAsync("d1 task", D1, D1);
        await _lifecycle.MorningCheckinAsync(D2, MorningValues(), []); // closes D1

        await Assert.ThrowsAsync<TrackerException>(() => _svc.SetDoneAsync(t.Id.ToString(), true, D2));
        await Assert.ThrowsAsync<TrackerException>(() => _svc.DropAsync(t.Id.ToString(), D2));
    }

    [Fact]
    public async Task Closing_freezes_counters_into_the_document()
    {
        var t = await _svc.AddAsync("d1 task", D1, D1);
        await _lifecycle.MorningCheckinAsync(D1, MorningValues(), []);
        await _svc.SetDoneAsync(t.Id.ToString(), true, D1);

        await _lifecycle.MorningCheckinAsync(D2, MorningValues(), []); // closes D1

        var d1 = await _lifecycle.GetEntryAsync(D1);
        Assert.Equal(1, d1!.QuickPlanned);
        Assert.Equal(1, d1.QuickDone); // frozen at close
    }

    [Fact]
    public async Task Dropped_tasks_leave_counters_and_range_queries()
    {
        var t = await _svc.AddAsync("dropped task", D2, D2);
        await _svc.DropAsync(t.Id.ToString(), D2);

        var list = await _svc.GetRangeAsync(D2, D2);
        Assert.Empty(list);
    }
}
