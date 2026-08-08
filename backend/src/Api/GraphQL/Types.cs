using DailyTracker.Api.Domain;

namespace DailyTracker.Api.GraphQL;

// ---- Enums (serialized as MORNING, NOT_DONE... per HotChocolate convention) ----

public enum Phase { Morning, Evening, Anytime }
public enum DayType { Workday, Weekend, Dayoff, Sick }
public enum DayStatus { Open, Closed, Partial, Missed }
public enum HabitState { Done, NotDone, NoData }

/// <summary>Maps GraphQL enums ↔ domain/BSON string constants (spec uses snake_case verbatim).</summary>
public static class GqlMap
{
    public static string ToDomain(this Phase p) => p switch
    {
        Phase.Morning => Phases.Morning,
        Phase.Evening => Phases.Evening,
        _ => Phases.Anytime,
    };

    public static string ToDomain(this DayType d) => d switch
    {
        DayType.Workday => DayTypes.Workday,
        DayType.Weekend => DayTypes.Weekend,
        DayType.Dayoff => DayTypes.Dayoff,
        _ => DayTypes.Sick,
    };

    public static string ToDomain(this HabitState s) => s switch
    {
        HabitState.Done => HabitStates.Done,
        HabitState.NotDone => HabitStates.NotDone,
        _ => HabitStates.NoData,
    };

    public static Phase PhaseFrom(string s) => s switch
    {
        Phases.Morning => Phase.Morning,
        Phases.Evening => Phase.Evening,
        _ => Phase.Anytime,
    };

    public static DayType DayTypeFrom(string s) => s switch
    {
        DayTypes.Weekend => DayType.Weekend,
        DayTypes.Dayoff => DayType.Dayoff,
        DayTypes.Sick => DayType.Sick,
        _ => DayType.Workday,
    };

    public static DayStatus StatusFrom(string s) => s switch
    {
        DayStatuses.Closed => DayStatus.Closed,
        DayStatuses.Partial => DayStatus.Partial,
        DayStatuses.Missed => DayStatus.Missed,
        _ => DayStatus.Open,
    };

    public static HabitState HabitStateFrom(string s) => s switch
    {
        HabitStates.Done => HabitState.Done,
        HabitStates.NotDone => HabitState.NotDone,
        _ => HabitState.NoData,
    };
}

// ---- Output DTOs ----

public sealed record MetricDefinitionDto(
    string Key, string Label, string Type, Phase Phase, int Order,
    VisibleWhenDto? VisibleWhen, int? DeferrableDays, int DayOffset,
    string? Polarity, ValidationDto? Validation, List<MetricOptionDto>? Options,
    int? MaxSelect, bool Active)
{
    public static MetricDefinitionDto From(MetricDefinition d) => new(
        d.Key, d.Label, d.Type, GqlMap.PhaseFrom(d.Phase), d.Order,
        d.VisibleWhen is null ? null : new VisibleWhenDto(d.VisibleWhen.Field, d.VisibleWhen.Values),
        d.DeferrableDays, d.DayOffset, d.Polarity,
        d.Validation is null ? null : new ValidationDto(d.Validation.Min, d.Validation.Max, d.Validation.Step, d.Validation.Required),
        d.Options?.Select(o => new MetricOptionDto(o.Value, o.Label)).ToList(),
        d.MaxSelect, d.Active);
}

public sealed record VisibleWhenDto(string Field, List<string> Values);
public sealed record ValidationDto(double? Min, double? Max, double? Step, bool? Required);
public sealed record MetricOptionDto(string Value, string Label);

public sealed record HabitDto(
    string Key, string Label, string ShortLabel, string Icon, string Measure,
    bool HasQuality, string? QualityLabel, bool Active, int Order)
{
    public static HabitDto From(Habit h) =>
        new(h.Key, h.Label, h.ShortLabel, h.Icon, h.Measure, h.HasQuality, h.QualityLabel, h.Active, h.Order);
}

public sealed record MetricValueDto(string Key, double? Number, string? Text, string? Time, List<string>? Options)
{
    public static MetricValueDto From(MetricValue v) => new(v.Key, v.Number, v.Text, v.Time, v.Options);
}

public sealed record HabitEntryDto(string HabitKey, HabitState State, double? Hours, int? Quality)
{
    public static HabitEntryDto From(HabitEntry h) =>
        new(h.HabitKey, GqlMap.HabitStateFrom(h.State), h.Hours, h.Quality);
}

public sealed record DailyEntryDto(
    string Date, DayStatus Status, DayType DayType,
    List<MetricValueDto> Values, List<HabitEntryDto> Habits,
    int? QuickPlanned, int QuickDone, int QuickAddedLater, int OngoingTouched,
    DateTime? MorningCheckinAt, DateTime? EveningCheckinAt)
{
    public static DailyEntryDto From(DailyEntry e) => new(
        e.Date, GqlMap.StatusFrom(e.Status), GqlMap.DayTypeFrom(e.DayType),
        e.Values.Select(MetricValueDto.From).ToList(),
        e.Habits.Select(HabitEntryDto.From).ToList(),
        e.QuickPlanned, e.QuickDone, e.QuickAddedLater, e.OngoingTouched,
        e.MorningCheckinAt, e.EveningCheckinAt);
}

public sealed record DeferredFieldDto(string Key, string Label, string BelongsToDate, string LastWritableDate)
{
    public static DeferredFieldDto From(DeferredField d) =>
        new(d.Key, d.Label, d.BelongsToDate, d.LastWritableDate);
}

/// <summary>The Today screen: entry + pending deferred fields.</summary>
public sealed record TodayPayload(DailyEntryDto Entry, List<DeferredFieldDto> Deferred);

public sealed record GoalDto(string Title, string Scope, string? TargetDate)
{
    public static GoalDto From(Goal g) => new(g.Title, g.Scope, g.TargetDate);
}

public sealed record TaskDto(
  string Id, string Title, string Category, string Kind, string Scope,
  string? PlannedDate, string Status, DateTime CreatedAt, DateTime? DoneAt)
{
    public static TaskDto From(TaskItem t) => new(
        t.Id.ToString(), t.Title, t.Category, t.Kind, t.Scope,
        t.PlannedDate, t.Status, t.CreatedAt, t.DoneAt);
}

// ---- Inputs ----

public sealed record MetricValueInput(string Key, double? Number, string? Text, string? Time, List<string>? Options)
{
    public MetricValue ToDomain() =>
        new() { Key = Key, Number = Number, Text = Text, Time = Time, Options = Options };
}
