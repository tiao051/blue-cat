using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DailyTracker.Api.Domain;

// Union values are stored as spec-literal strings (scale, not_done, multi_enum...)
// instead of C# enums — BSON matches spec §6 verbatim; the GraphQL layer maps enums.
public static class MetricTypes
{
    public const string Scale = "scale";
    public const string Number = "number";
    public const string Time = "time";
    public const string Enum = "enum";
    public const string MultiEnum = "multi_enum";
    public const string Text = "text";
}

public static class Phases
{
    public const string Morning = "morning";
    public const string Evening = "evening";
    public const string Anytime = "anytime";
}

public static class DayTypes
{
    public const string Workday = "workday";
    public const string Weekend = "weekend";
    public const string Dayoff = "dayoff";
    public const string Sick = "sick";
}

public static class DayStatuses
{
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Partial = "partial";
    public const string Missed = "missed";
}

public static class HabitStates
{
    public const string Done = "done";
    public const string NotDone = "not_done";
    public const string NoData = "no_data";
}

public static class Polarities
{
    public const string HigherBetter = "higher_better";
    public const string HigherWorse = "higher_worse";
}

/// <summary>Spec §5. A tracked metric — forms are built from these, never hardcoded.</summary>
public sealed class MetricDefinition
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    /// <summary>Identifier; its meaning never changes (versioning rule §5).</summary>
    public required string Key { get; set; }

    public required string Label { get; set; }
    public required string Type { get; set; }
    public required string Phase { get; set; }
    public int Order { get; set; }

    /// <summary>Visibility condition — a simple value match, no DSL (§5).</summary>
    public VisibleWhen? VisibleWhen { get; set; }

    /// <summary>Days the value stays writable after its owning day. Objective data only (§5).</summary>
    public int? DeferrableDays { get; set; }

    /// <summary>
    /// A value entered at day D's check-in is written to the document of day D + dayOffset.
    /// screen_time: -1 (belongs to yesterday). Addendum beyond the §5 table — see README.
    /// </summary>
    public int DayOffset { get; set; }

    public string? Polarity { get; set; }
    public MetricValidation? Validation { get; set; }
    public List<MetricOption>? Options { get; set; }
    public int? MaxSelect { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class VisibleWhen
{
    public required string Field { get; set; }
    public required List<string> Values { get; set; }
}

public sealed class MetricValidation
{
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Step { get; set; }
    public bool? Required { get; set; }
}

/// <summary>enum/multi_enum option: value is immutable (carries meaning), label changes freely.</summary>
public sealed class MetricOption
{
    public required string Value { get; set; }
    public required string Label { get; set; }
}

/// <summary>Spec §6 `habits`.</summary>
public sealed class Habit
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    /// <summary>Immutable identifier, referenced from daily_entries and habit_targets.</summary>
    public required string Key { get; set; }

    public required string Label { get; set; }

    /// <summary>~8 chars max, used only in the grid cell.</summary>
    public required string ShortLabel { get; set; }

    public required string Icon { get; set; }

    /// <summary>binary or duration.</summary>
    public required string Measure { get; set; }

    public bool HasQuality { get; set; }
    public string? QualityLabel { get; set; }
    public bool Active { get; set; } = true;
    public int Order { get; set; }
}

public static class HabitMeasures
{
    public const string Binary = "binary";
    public const string Duration = "duration";
}

/// <summary>Spec §6 `habit_targets`. Changing a target = new record with a new effectiveFrom; never edit old ones.</summary>
public sealed class HabitTarget
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public required string HabitKey { get; set; }

    /// <summary>Only "week" (§6).</summary>
    public string Period { get; set; } = "week";

    public double Target { get; set; }

    /// <summary>sessions or hours.</summary>
    public required string Unit { get; set; }

    /// <summary>First ISO week this target applies to, e.g. "2026-W32".</summary>
    public required string EffectiveFrom { get; set; }
}

/// <summary>Spec §6 `daily_entries` — one document per day, date is the unique key.</summary>
public sealed class DailyEntry
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    /// <summary>yyyy-MM-dd in local time (§10) — unique index.</summary>
    public required string Date { get; set; }

    public string Status { get; set; } = DayStatuses.Open;
    public string DayType { get; set; } = DayTypes.Workday;

    /// <summary>Dynamic key-value bag — keys come from metric_definitions.</summary>
    public List<MetricValue> Values { get; set; } = [];

    public List<HabitEntry> Habits { get; set; } = [];

    /// <summary>Denominator — locked when morning check-in completes, never grows. null = no_data (no morning check-in).</summary>
    public int? QuickPlanned { get; set; }

    public int QuickDone { get; set; }
    public int QuickAddedLater { get; set; }
    public int OngoingTouched { get; set; }

    public DateTime? MorningCheckinAt { get; set; }
    public DateTime? EveningCheckinAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Per-field update timestamps — last-write-wins on sync (§10).</summary>
    public Dictionary<string, DateTime> FieldUpdatedAt { get; set; } = [];

    /// <summary>
    /// Keys of deferred ("later") fields that belong to this day and are still unfilled (spec §5).
    /// Pure marker — does not keep the day pending and does not count as data.
    /// </summary>
    public List<string> Deferred { get; set; } = [];
}

/// <summary>
/// One metric value: the slot matching its type is filled, the rest stay null (dropped on serialize).
/// Mongo stores exactly the shape GraphQL serves — spec §10 "typed slots per pair".
/// </summary>
public sealed class MetricValue
{
    public required string Key { get; set; }
    public double? Number { get; set; }
    public string? Text { get; set; }

    /// <summary>"HH:mm" for the time type.</summary>
    public string? Time { get; set; }

    public List<string>? Options { get; set; }
}

/// <summary>state always has 3 values, even for duration habits; hours 0 is real data ≠ no_data (§6).</summary>
public sealed class HabitEntry
{
    public required string HabitKey { get; set; }
    public string State { get; set; } = HabitStates.NoData;
    public double? Hours { get; set; }

    /// <summary>Only exists when the habit has quality scoring and state is done.</summary>
    public int? Quality { get; set; }
}

/// <summary>Spec §6 `tasks`. v1: scope day/week only.</summary>
public sealed class TaskItem
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public required string Title { get; set; }

    /// <summary>personal or work — work never enters analysis (§6).</summary>
    public required string Category { get; set; }

    /// <summary>quick or ongoing.</summary>
    public required string Kind { get; set; }

    /// <summary>day · week (month deferred to v2).</summary>
    public required string Scope { get; set; }

    /// <summary>A concrete date or an ISO week code.</summary>
    public required string ScopeKey { get; set; }

    /// <summary>Only present when scope is day.</summary>
    public string? PlannedDate { get; set; }

    public string Status { get; set; } = "todo";
    public string? OriginalDate { get; set; }
    public int CarryCount { get; set; }
    public List<string> TouchedDates { get; set; } = [];
    public DateTime? DoneAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Spec §6 `goals`. v1: one seeded year goal, read-only.</summary>
public sealed class Goal
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public required string Title { get; set; }

    /// <summary>year or month.</summary>
    public required string Scope { get; set; }

    public string? TargetDate { get; set; }
    public ObjectId? ParentId { get; set; }
    public string Status { get; set; } = "active";
    public bool Active { get; set; } = true;
}
