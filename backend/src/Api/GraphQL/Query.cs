using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using MongoDB.Driver;

namespace DailyTracker.Api.GraphQL;

public class Query
{
    /// <summary>
    /// Definitions đang active, lọc theo phase và dayType (evaluate visibleWhen — spec §5).
    /// Frontend dựng form từ đây, không hardcode.
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

    /// <summary>visibleWhen là phép khớp giá trị đơn giản — v1 chỉ biết field dayType (spec §5, không DSL).</summary>
    private static bool IsVisible(MetricDefinition def, string dayType) =>
        def.VisibleWhen is null
        || def.VisibleWhen.Field != "dayType"
        || def.VisibleWhen.Values.Contains(dayType);

    public async Task<List<HabitDto>> GetHabits(MongoContext ctx, CancellationToken ct) =>
        (await ctx.Habits.Find(h => h.Active).SortBy(h => h.Order).ToListAsync(ct))
        .Select(HabitDto.From).ToList();

    /// <summary>Entry một ngày. clientDate để lazy-close và synthesize missed cho ngày quá khứ không có doc.</summary>
    public async Task<DailyEntryDto> GetDailyEntry(
        string date, string clientDate, DayLifecycleService lifecycle, CancellationToken ct)
    {
        await lifecycle.EnsureClosedThroughAsync(clientDate, ct);
        return DailyEntryDto.From(await lifecycle.GetOrSynthesizeAsync(date, clientDate, ct));
    }

    /// <summary>Màn Hôm nay: entry của date + các field để-sau còn hạn (spec §9.2).</summary>
    public async Task<TodayPayload> GetToday(string date, DayLifecycleService lifecycle, CancellationToken ct)
    {
        await lifecycle.EnsureClosedThroughAsync(date, ct);
        var entry = await lifecycle.GetOrSynthesizeAsync(date, date, ct);
        var deferred = await lifecycle.GetDeferredAsync(date, ct);
        return new TodayPayload(DailyEntryDto.From(entry), deferred.Select(DeferredFieldDto.From).ToList());
    }

    /// <summary>Một dòng mục tiêu năm read-only (R12, v1).</summary>
    public async Task<GoalDto?> GetYearGoal(MongoContext ctx, CancellationToken ct)
    {
        var goal = await ctx.Goals.Find(g => g.Scope == "year" && g.Active).FirstOrDefaultAsync(ct);
        return goal is null ? null : GoalDto.From(goal);
    }
}
