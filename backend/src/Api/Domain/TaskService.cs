using DailyTracker.Api.Data;
using MongoDB.Bson;
using MongoDB.Driver;
// NOTE: quick numerator / added-after-lock counting lives in DayLifecycleService.QuickCountersAsync
// (it belongs to the day lifecycle — frozen into the document at close time).

namespace DailyTracker.Api.Domain;

/// <summary>
/// Quick tasks (spec §6 tasks). v1: personal + quick + scope day.
/// Tasks of a closed day are locked — same rule as metrics (R18).
/// </summary>
public sealed class TaskService(MongoContext ctx, DayLifecycleService lifecycle)
{
    public async Task<List<TaskItem>> GetRangeAsync(string from, string to, CancellationToken ct = default)
    {
        GuardDate(from);
        GuardDate(to);
        var filter = Builders<TaskItem>.Filter.Gte(t => t.PlannedDate, from)
                     & Builders<TaskItem>.Filter.Lte(t => t.PlannedDate, to)
                     & Builders<TaskItem>.Filter.Ne(t => t.Status, "dropped");
        return await ctx.Tasks.Find(filter).SortBy(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<TaskItem> AddAsync(string title, string plannedDate, string clientDate, CancellationToken ct = default)
    {
        GuardDate(plannedDate);
        GuardDate(clientDate);
        if (string.IsNullOrWhiteSpace(title))
            throw new TrackerException("Task title can't be empty.");
        if (LocalDate.Compare(plannedDate, clientDate) < 0)
            throw new TrackerException("Can't add tasks to a past day (spec R18).");

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = title.Trim(),
            Category = "personal",
            Kind = "quick",
            Scope = "day",
            ScopeKey = plannedDate,
            PlannedDate = plannedDate,
            OriginalDate = plannedDate,
            Status = "todo",
            CreatedAt = now,
            UpdatedAt = now,
        };
        await ctx.Tasks.InsertOneAsync(task, cancellationToken: ct);
        return task;
    }

    public async Task<TaskItem> SetDoneAsync(string id, bool done, string clientDate, CancellationToken ct = default)
    {
        var task = await FindWritableAsync(id, clientDate, ct);
        task.Status = done ? "done" : "todo";
        task.DoneAt = done ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;
        await ctx.Tasks.ReplaceOneAsync(t => t.Id == task.Id, task, cancellationToken: ct);
        return task;
    }

    public async Task<TaskItem> DropAsync(string id, string clientDate, CancellationToken ct = default)
    {
        var task = await FindWritableAsync(id, clientDate, ct);
        task.Status = "dropped";
        task.UpdatedAt = DateTime.UtcNow;
        await ctx.Tasks.ReplaceOneAsync(t => t.Id == task.Id, task, cancellationToken: ct);
        return task;
    }

    private async Task<TaskItem> FindWritableAsync(string id, string clientDate, CancellationToken ct)
    {
        GuardDate(clientDate);
        if (!ObjectId.TryParse(id, out var oid))
            throw new TrackerException($"Invalid task id: '{id}'.");

        var task = await ctx.Tasks.Find(t => t.Id == oid).FirstOrDefaultAsync(ct)
            ?? throw new TrackerException("Task not found.");

        // Tasks of a closed day are locked (spec §7 v3.2)
        if (task.PlannedDate is string date)
        {
            var entry = await lifecycle.GetOrSynthesizeAsync(date, clientDate, ct);
            if (entry.Status is not (DayStatuses.Open))
                throw new TrackerException($"Day {date} is closed — its tasks can't be edited.");
        }

        return task;
    }

    private static void GuardDate(string date)
    {
        if (!LocalDate.IsValid(date))
            throw new TrackerException($"Date '{date}' is not in yyyy-MM-dd format.");
    }
}
