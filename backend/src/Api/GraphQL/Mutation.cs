using DailyTracker.Api.Domain;

namespace DailyTracker.Api.GraphQL;

public class Mutation
{
    /// <summary>
    /// Check-in sáng ngày D: ghi values theo ngày sở hữu, đánh dấu để-sau,
    /// chốt mẫu số, đóng sổ D-1 (spec §7).
    /// </summary>
    public async Task<DailyEntryDto> MorningCheckin(
        string date, List<MetricValueInput> values, List<string> deferredKeys,
        DayLifecycleService lifecycle, CancellationToken ct) =>
        DailyEntryDto.From(await lifecycle.MorningCheckinAsync(
            date, values.Select(v => v.ToDomain()).ToList(), deferredKeys, ct));

    public async Task<DailyEntryDto> EveningCheckin(
        string date, List<MetricValueInput> values,
        DayLifecycleService lifecycle, CancellationToken ct) =>
        DailyEntryDto.From(await lifecycle.EveningCheckinAsync(
            date, values.Select(v => v.ToDomain()).ToList(), ct));

    /// <summary>Sửa giá trị ngày chưa đóng + điền field để-sau (date = ngày sở hữu giá trị).</summary>
    public async Task<DailyEntryDto> SetMetricValue(
        string date, MetricValueInput value, string clientDate,
        DayLifecycleService lifecycle, CancellationToken ct) =>
        DailyEntryDto.From(await lifecycle.SetMetricValueAsync(date, value.ToDomain(), clientDate, ct));

    public async Task<DailyEntryDto> SetHabit(
        string date, string habitKey, HabitState state, double? hours, int? quality,
        DayLifecycleService lifecycle, CancellationToken ct) =>
        DailyEntryDto.From(await lifecycle.SetHabitAsync(date, habitKey, state.ToDomain(), hours, quality, ct));

    public async Task<DailyEntryDto> SetDayType(
        string date, DayType dayType,
        DayLifecycleService lifecycle, CancellationToken ct) =>
        DailyEntryDto.From(await lifecycle.SetDayTypeAsync(date, dayType.ToDomain(), ct));

    // ---- Việc vụn (spec §6 tasks — kéo M2 lên sớm) ----

    public async Task<TaskDto> AddTask(
        string title, string plannedDate, string clientDate,
        TaskService tasks, CancellationToken ct) =>
        TaskDto.From(await tasks.AddAsync(title, plannedDate, clientDate, ct));

    public async Task<TaskDto> SetTaskDone(
        string id, bool done, string clientDate,
        TaskService tasks, CancellationToken ct) =>
        TaskDto.From(await tasks.SetDoneAsync(id, done, clientDate, ct));

    public async Task<TaskDto> DropTask(
        string id, string clientDate,
        TaskService tasks, CancellationToken ct) =>
        TaskDto.From(await tasks.DropAsync(id, clientDate, ct));
}
