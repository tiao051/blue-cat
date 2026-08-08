using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using MongoDB.Driver;

namespace DailyTracker.Api.Migrations;

/// <summary>
/// Seed per spec §8: 11 metrics + 5 habits + 5 starter weekly targets + 1 year goal.
/// Every write is an upsert by natural key (key/title) — re-running never duplicates (M0 checklist).
/// Labels are display-only and may change freely (§5); keys and option values are immutable.
/// </summary>
public sealed class M0002_SeedV1 : IMigration
{
    public string Id => "M0002_SeedV1";

    private static MetricValidation Scale1To10 => new() { Min = 1, Max = 10, Step = 1, Required = true };

    public async Task UpAsync(IMongoDatabase db, CancellationToken ct)
    {
        // ---- 11 tracked metrics ----
        MetricDefinition[] metrics =
        [
            // Morning
            new()
            {
                Key = "sleep_start", Label = "Bedtime last night", Type = MetricTypes.Time,
                Phase = Phases.Morning, Order = 10,
                Validation = new MetricValidation { Required = true },
            },
            new()
            {
                Key = "sleep_end", Label = "Wake-up time", Type = MetricTypes.Time,
                Phase = Phases.Morning, Order = 20,
                Validation = new MetricValidation { Required = true },
            },
            new()
            {
                Key = "screen_time", Label = "Screen time (hours)", Type = MetricTypes.Number,
                Phase = Phases.Morning, Order = 30,
                // Belongs to yesterday (dayOffset -1), writable through today (deferrable 1) — spec §5/§8
                DayOffset = -1, DeferrableDays = 1, Polarity = Polarities.HigherWorse,
                Validation = new MetricValidation { Min = 0, Max = 24, Step = 0.5, Required = true },
            },
            new()
            {
                Key = "mood_morning", Label = "Morning mood", Type = MetricTypes.Scale,
                Phase = Phases.Morning, Order = 40,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },

            // Evening
            new()
            {
                Key = "productivity", Label = "Productivity", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 10,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                Key = "mood_evening", Label = "Evening mood", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 20,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                Key = "physical", Label = "Physical condition", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 30,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                // Sick days still ask about recovery (spec v3.2)
                Key = "recovery", Label = "Did you actually recover?", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 40,
                VisibleWhen = new VisibleWhen { Field = "dayType", Values = [DayTypes.Weekend, DayTypes.Dayoff, DayTypes.Sick] },
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                // Not asked on sick days (spec v3.2)
                Key = "time_meaningful", Label = "Was your free time meaningful?", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 50,
                VisibleWhen = new VisibleWhen { Field = "dayType", Values = [DayTypes.Weekend, DayTypes.Dayoff] },
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                Key = "attention_main", Label = "Where was your mind today?", Type = MetricTypes.MultiEnum,
                Phase = Phases.Evening, Order = 60,
                MaxSelect = 2, // keeps the word "mainly" meaningful — Appendix A
                Options =
                [
                    new MetricOption { Value = "work", Label = "Work" },
                    new MetricOption { Value = "learning", Label = "Learning & growth" },
                    new MetricOption { Value = "phone", Label = "Phone & entertainment" },
                    new MetricOption { Value = "social", Label = "Social & other people" },
                    new MetricOption { Value = "empty", Label = "Empty" },
                ],
                Validation = new MetricValidation { Required = true },
            },
            new()
            {
                Key = "note", Label = "Notes", Type = MetricTypes.Text,
                Phase = Phases.Evening, Order = 70,
                Validation = new MetricValidation { Required = false },
            },
        ];

        var metricCol = db.GetCollection<MetricDefinition>(CollectionNames.MetricDefinitions);
        foreach (var m in metrics)
            await metricCol.ReplaceOneAsync(d => d.Key == m.Key, m, new ReplaceOptions { IsUpsert = true }, ct);

        // ---- 5 habits ----
        Habit[] habits =
        [
            new()
            {
                Key = "gym", Label = "Gym / exercise", ShortLabel = "gym", Icon = "barbell",
                Measure = HabitMeasures.Binary, HasQuality = true, QualityLabel = "Was it a good session?", Order = 10,
            },
            new()
            {
                Key = "reading", Label = "Reading", ShortLabel = "read", Icon = "book",
                Measure = HabitMeasures.Duration, Order = 20,
            },
            new()
            {
                // Only counts time outside official work hours (§8 convention)
                Key = "tech", Label = "Tech learning (off-hours)", ShortLabel = "tech", Icon = "code",
                Measure = HabitMeasures.Duration, Order = 30,
            },
            new()
            {
                Key = "rp", Label = "RP practice", ShortLabel = "RP", Icon = "microphone",
                Measure = HabitMeasures.Duration, Order = 40,
            },
            new()
            {
                Key = "go_out", Label = "Go outside / meet people", ShortLabel = "out", Icon = "door-exit",
                Measure = HabitMeasures.Binary, Order = 50,
            },
        ];

        var habitCol = db.GetCollection<Habit>(CollectionNames.Habits);
        foreach (var h in habits)
            await habitCol.ReplaceOneAsync(d => d.Key == h.Key, h, new ReplaceOptions { IsUpsert = true }, ct);

        // ---- Starter weekly targets (reasonable guesses — user adjusts later; new value = new effectiveFrom) ----
        var thisWeek = LocalDate.IsoWeek(LocalDate.ToDateString(DateOnly.FromDateTime(DateTime.Now)));
        (string HabitKey, double Target, string Unit)[] targets =
        [
            ("gym", 3, "sessions"),
            ("reading", 3, "hours"),
            ("tech", 3, "hours"),
            ("rp", 2, "hours"),
            ("go_out", 2, "sessions"),
        ];

        var targetCol = db.GetCollection<HabitTarget>(CollectionNames.HabitTargets);
        foreach (var (habitKey, target, unit) in targets)
        {
            var doc = new HabitTarget { HabitKey = habitKey, Target = target, Unit = unit, EffectiveFrom = thisWeek };
            // Upsert by habitKey: the seed only guarantees one starter target per habit
            await targetCol.ReplaceOneAsync(t => t.HabitKey == habitKey, doc, new ReplaceOptions { IsUpsert = true }, ct);
        }

        // ---- 1 year goal (v1: read-only single reminder line — R12) ----
        // Upsert by scope: v1 has exactly one year goal, so a label change replaces it
        // instead of inserting a second document
        var goal = new Goal { Title = "Study abroad", Scope = "year" };
        await db.GetCollection<Goal>(CollectionNames.Goals)
            .ReplaceOneAsync(g => g.Scope == "year", goal, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
