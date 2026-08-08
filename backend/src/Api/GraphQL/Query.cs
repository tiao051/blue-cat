using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using MongoDB.Driver;

namespace DailyTracker.Api.GraphQL;

public class Query
{
    /// <summary>
    /// Active definitions, filtered by phase and dayType (evaluating visibleWhen — spec §5).
    /// The frontend builds forms from these; nothing is hardcoded.
    /// </summary>
    public async Task<List<MetricDefinitionDto>> GetMetricDefinitions(
        Phase? phase, DayType? dayType, MongoContext ctx, CancellationToken ct)
    {
        var defs = await ctx.MetricDefinitions.Find(d => d.Active).ToListAsync(ct);

        IEnumerable<MetricDefinition> filtered = defs;
        if (phase is not null)
            filtered = filtered.Where(d => d.Phase == phase.Value.ToDomain());
        if (dayType is not null)
            filtered = filtered.Where(d => IsVisible(d, dayType.Value.ToDomain()));

        return filtered.OrderBy(d => d.Order).Select(MetricDefinitionDto.From).ToList();
    }

    /// <summary>visibleWhen is a simple value match — v1 only knows the dayType field (spec §5, no DSL).</summary>
    private static bool IsVisible(MetricDefinition def, string dayType) =>
        def.VisibleWhen is null
        || def.VisibleWhen.Field != "dayType"
        || def.VisibleWhen.Values.Contains(dayType);

    public async Task<List<HabitDto>> GetHabits(MongoContext ctx, CancellationToken ct) =>
        (await ctx.Habits.Find(h => h.Active).SortBy(h => h.Order).ToListAsync(ct))
        .Select(HabitDto.From).ToList();

    /// <summary>One day's entry. clientDate drives lazy closing and synthesizes missed for doc-less past days.</summary>
    public async Task<DailyEntryDto> GetDailyEntry(
        string date, string clientDate, DayLifecycleService lifecycle, CancellationToken ct)
    {
        await lifecycle.EnsureClosedThroughAsync(clientDate, ct);
        return DailyEntryDto.From(await lifecycle.GetOrSynthesizeAsync(date, clientDate, ct));
    }

    /// <summary>The Today screen: date's entry + deferred fields still inside their window (spec §9.2).</summary>
    public async Task<TodayPayload> GetToday(
        string date, DayLifecycleService lifecycle, TaskService tasks, CancellationToken ct)
    {
        await lifecycle.EnsureClosedThroughAsync(date, ct);
        var entry = await lifecycle.GetOrSynthesizeAsync(date, date, ct);
        var deferred = await lifecycle.GetDeferredAsync(date, ct);

        var dto = DailyEntryDto.From(entry);
        if (entry.Status == DayStatuses.Open)
        {
            // Open day: numerator + added-after-lock computed live from tasks (frozen only at close)
            var (done, addedLater) = await lifecycle.QuickCountersAsync(entry, ct);
            dto = dto with { QuickDone = done, QuickAddedLater = addedLater };
        }

        return new TodayPayload(dto, deferred.Select(DeferredFieldDto.From).ToList());
    }

    /// <summary>Tasks within a date range (yesterday/today/tomorrow on the Today screen).</summary>
    public async Task<List<TaskDto>> GetTasks(string from, string to, TaskService tasks, CancellationToken ct) =>
        (await tasks.GetRangeAsync(from, to, ct)).Select(TaskDto.From).ToList();

    /// <summary>The single read-only year goal line (R12, v1).</summary>
    public async Task<GoalDto?> GetYearGoal(MongoContext ctx, CancellationToken ct)
    {
        var goal = await ctx.Goals.Find(g => g.Scope == "year" && g.Active).FirstOrDefaultAsync(ct);
        return goal is null ? null : GoalDto.From(goal);
    }
}
