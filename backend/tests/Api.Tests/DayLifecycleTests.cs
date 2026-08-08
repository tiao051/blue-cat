using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using DailyTracker.Api.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Xunit;

namespace DailyTracker.Api.Tests;

/// <summary>
/// Day lifecycle integration tests (spec §7) — needs local Mongo (docker tracker-mongo, port 27018).
/// Each test gets its own database, seeded by the real migrations, dropped on dispose.
/// </summary>
public sealed class DayLifecycleTests : IAsyncLifetime
{
    private const string Uri = "mongodb://localhost:27018";
    private readonly string _dbName = $"tracker_test_{Guid.NewGuid():N}";
    private MongoContext _ctx = null!;
    private DayLifecycleService _svc = null!;

    // Fixed test dates: Thu 06/08, Fri 07/08, Sat 08/08/2026
    private const string D0 = "2026-08-06";
    private const string D1 = "2026-08-07";
    private const string D2 = "2026-08-08";

    public async Task InitializeAsync()
    {
        _ctx = new MongoContext(Uri, _dbName);
        await MigrationRunner.RunAsync(_ctx.Database, NullLogger.Instance);
        _svc = new DayLifecycleService(_ctx, new MetricValidationService());
    }

    public async Task DisposeAsync() => await _ctx.Client.DropDatabaseAsync(_dbName);

    private static List<MetricValue> MorningValues(double? screenTime = null)
    {
        List<MetricValue> values =
        [
            new() { Key = "sleep_start", Time = "23:30" },
            new() { Key = "sleep_end", Time = "07:00" },
            new() { Key = "mood_morning", Number = 7 },
        ];
        if (screenTime is not null)
            values.Add(new MetricValue { Key = "screen_time", Number = screenTime });
        return values;
    }

    private static List<MetricValue> EveningValues() =>
    [
        new() { Key = "productivity", Number = 6 },
        new() { Key = "mood_evening", Number = 7 },
        new() { Key = "physical", Number = 8 },
        new() { Key = "attention_main", Options = ["work", "learning"] },
    ];

    // ---------- Closing via next day's morning check-in (spec §7) ----------

    [Fact]
    public async Task Morning_checkin_D2_closes_D1_as_closed_when_both_checkins_present()
    {
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        await _svc.EveningCheckinAsync(D1, EveningValues());

        await _svc.MorningCheckinAsync(D2, MorningValues(), []);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(DayStatuses.Closed, d1!.Status);
        Assert.NotNull(d1.ClosedAt);
    }

    [Fact]
    public async Task Morning_checkin_D2_closes_D1_as_partial_when_evening_missing()
    {
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        // no evening check-in on D1

        await _svc.MorningCheckinAsync(D2, MorningValues(), []);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(DayStatuses.Partial, d1!.Status);
    }

    [Fact]
    public async Task Day_without_doc_synthesizes_as_missed()
    {
        var entry = await _svc.GetOrSynthesizeAsync(D0, D2);
        Assert.Equal(DayStatuses.Missed, entry.Status);

        var today = await _svc.GetOrSynthesizeAsync(D2, D2);
        Assert.Equal(DayStatuses.Open, today.Status);
        Assert.Equal(DayTypes.Weekend, today.DayType); // 08/08/2026 is a Saturday
    }

    [Fact]
    public async Task EnsureClosedThrough_closes_stale_days_by_their_data()
    {
        await _svc.MorningCheckinAsync(D0, MorningValues(), []);
        await _svc.EveningCheckinAsync(D0, EveningValues());

        await _svc.EnsureClosedThroughAsync(D2); // D0 <= D2-2 → close

        var d0 = await _svc.GetEntryAsync(D0);
        Assert.Equal(DayStatuses.Closed, d0!.Status);
    }

    [Fact]
    public async Task Doc_with_only_deferral_marker_and_no_data_closes_as_missed()
    {
        // D1: D2's morning check-in defers screen_time → creates a marker on D1's doc, nothing else
        await _svc.MorningCheckinAsync(D2, MorningValues(), ["screen_time"]);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.NotNull(d1);
        Assert.Contains("screen_time", d1!.Deferred);

        // Two days later D1 gets lazily closed → missed, since it holds no real data
        await _svc.EnsureClosedThroughAsync("2026-08-10");
        d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(DayStatuses.Missed, d1!.Status);
    }

    // ---------- Denominator locking (spec §6) ----------

    [Fact]
    public async Task QuickPlanned_locks_on_first_checkin_and_never_changes()
    {
        await _ctx.Tasks.InsertManyAsync(
        [
            new TaskItem { Title = "a", Category = "personal", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
            new TaskItem { Title = "b", Category = "personal", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
            new TaskItem { Title = "work", Category = "work", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
            new TaskItem { Title = "ongoing", Category = "personal", Kind = "ongoing", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
        ]);

        var entry = await _svc.MorningCheckinAsync(D2, MorningValues(), []);
        Assert.Equal(2, entry.QuickPlanned); // personal + quick only

        // Add a task after the morning check-in, then re-submit (same-day edit) — denominator stays
        await _ctx.Tasks.InsertOneAsync(
            new TaskItem { Title = "c", Category = "personal", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 });
        entry = await _svc.MorningCheckinAsync(D2, MorningValues(), []);
        Assert.Equal(2, entry.QuickPlanned);
    }

    [Fact]
    public async Task No_morning_checkin_means_quickPlanned_null()
    {
        await _svc.EveningCheckinAsync(D2, EveningValues());
        var entry = await _svc.GetEntryAsync(D2);
        Assert.Null(entry!.QuickPlanned);
    }

    // ---------- Owning day + deferral (spec §5, §8) ----------

    [Fact]
    public async Task Screen_time_entered_on_D2_lands_in_D1_document()
    {
        await _svc.MorningCheckinAsync(D2, MorningValues(screenTime: 4.5), []);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.NotNull(d1);
        Assert.Equal(4.5, d1!.Values.Single(v => v.Key == "screen_time").Number);

        var d2 = await _svc.GetEntryAsync(D2);
        Assert.DoesNotContain(d2!.Values, v => v.Key == "screen_time");
        Assert.Equal(7, d2.Values.Single(v => v.Key == "mood_morning").Number);
    }

    [Fact]
    public async Task Deferred_field_shows_owning_date_and_is_fillable_inside_window()
    {
        await _svc.MorningCheckinAsync(D2, MorningValues(), ["screen_time"]);

        var deferred = await _svc.GetDeferredAsync(D2);
        var field = Assert.Single(deferred);
        Assert.Equal("screen_time", field.Key);
        Assert.Equal(D1, field.BelongsToDate);      // belongs to yesterday
        Assert.Equal(D2, field.LastWritableDate);   // deferrable for 1 day

        // Fill inside the window — lands in D1's doc
        await _svc.SetMetricValueAsync(D1, new MetricValue { Key = "screen_time", Number = 3 }, D2);
        var d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(3, d1!.Values.Single(v => v.Key == "screen_time").Number);
        Assert.Empty(d1.Deferred);
        Assert.Empty(await _svc.GetDeferredAsync(D2));
    }

    [Fact]
    public async Task Past_deferral_window_rejects_writes()
    {
        await _svc.MorningCheckinAsync(D2, MorningValues(), ["screen_time"]);

        // By 10/08 (past D1 + 1 day) the window is gone
        var ex = await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D1, new MetricValue { Key = "screen_time", Number = 3 }, "2026-08-10"));
        Assert.Contains("window passed", ex.Message);

        // And it no longer shows in the pending list
        Assert.Empty(await _svc.GetDeferredAsync("2026-08-10"));
    }

    // ---------- Closed days are locked (spec §7 v3.2, R18) ----------

    [Fact]
    public async Task Closed_day_rejects_normal_metric_and_habit_writes()
    {
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        await _svc.EveningCheckinAsync(D1, EveningValues());
        await _svc.MorningCheckinAsync(D2, MorningValues(), []); // closes D1

        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D1, new MetricValue { Key = "mood_evening", Number = 9 }, D2));

        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D1, "gym", HabitStates.Done, null, null));

        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.EveningCheckinAsync(D1, EveningValues()));
    }

    [Fact]
    public async Task Open_day_edits_freely()
    {
        await _svc.EveningCheckinAsync(D2, EveningValues());
        // Fat-fingered the mood at 9pm — editable because the day hasn't closed (spec v3.2)
        await _svc.SetMetricValueAsync(D2, new MetricValue { Key = "mood_evening", Number = 3 }, D2);

        var entry = await _svc.GetEntryAsync(D2);
        Assert.Equal(3, entry!.Values.Single(v => v.Key == "mood_evening").Number);
    }

    // ---------- Habits: 3 states, hours 0 ≠ no_data, quality gating (spec §6) ----------

    [Fact]
    public async Task Habit_hours_zero_is_real_data()
    {
        await _svc.SetHabitAsync(D2, "reading", HabitStates.NotDone, 0, null);

        var entry = await _svc.GetEntryAsync(D2);
        var reading = entry!.Habits.Single(h => h.HabitKey == "reading");
        Assert.Equal(HabitStates.NotDone, reading.State);
        Assert.Equal(0, reading.Hours); // a real 0, not null
    }

    [Fact]
    public async Task Habit_quality_only_when_done_and_scored()
    {
        // gym has quality, state done → OK
        await _svc.SetHabitAsync(D2, "gym", HabitStates.Done, null, 8);
        var entry = await _svc.GetEntryAsync(D2);
        Assert.Equal(8, entry!.Habits.Single(h => h.HabitKey == "gym").Quality);

        // reading has no quality score
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "reading", HabitStates.Done, 1, 8));

        // gym quality while not_done
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "gym", HabitStates.NotDone, null, 5));

        // binary habits take no hours
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "gym", HabitStates.Done, 1.5, null));

        // no_data can't carry hours
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "reading", HabitStates.NoData, 0, null));
    }

    // ---------- Validation (spec §5) ----------

    [Fact]
    public async Task Validation_rejects_bad_input_and_writes_nothing()
    {
        // scale out of range
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "mood_evening", Number = 11 }, D2));

        // multi_enum over maxSelect 2
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2,
                new MetricValue { Key = "attention_main", Options = ["work", "learning", "phone"] }, D2));

        // unknown option
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "attention_main", Options = ["gaming"] }, D2));

        // unknown key
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "nope", Number = 1 }, D2));

        // wrong slot: text on a scale
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "mood_evening", Text = "9" }, D2));

        Assert.Null(await _svc.GetEntryAsync(D2)); // nothing written
    }

    [Fact]
    public async Task DayType_changes_and_defaults_by_calendar()
    {
        var entry = await _svc.SetDayTypeAsync(D2, DayTypes.Dayoff);
        Assert.Equal(DayTypes.Dayoff, entry.DayType);

        // D1 (Friday) defaults to workday when its doc is created via check-in
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        Assert.Equal(DayTypes.Workday, (await _svc.GetEntryAsync(D1))!.DayType);
    }
}
