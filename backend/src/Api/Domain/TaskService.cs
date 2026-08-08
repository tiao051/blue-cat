using DailyTracker.Api.Data;
using MongoDB.Bson;
using MongoDB.Driver;
// LƯU Ý: tính tử số/việc-thêm-sau nằm ở DayLifecycleService.QuickCountersAsync
// (nó thuộc vòng đời ngày — chốt cứng lúc đóng sổ).

namespace DailyTracker.Api.Domain;

/// <summary>
/// Việc vụn (spec §6 tasks). v1: personal + quick + scope day.
/// Việc của ngày đã đóng thì khoá — cùng luật với metric (R18).
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
            throw new TrackerException("Tên việc không được để trống.");
        if (LocalDate.Compare(plannedDate, clientDate) < 0)
            throw new TrackerException("Không thêm việc cho ngày đã qua (spec R18).");

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
            throw new TrackerException($"Id việc không hợp lệ: '{id}'.");

        var task = await ctx.Tasks.Find(t => t.Id == oid).FirstOrDefaultAsync(ct)
            ?? throw new TrackerException("Không tìm thấy việc này.");

        // Ngày đã đóng thì việc của ngày đó khoá (spec §7 v3.2)
        if (task.PlannedDate is string date)
        {
            var entry = await lifecycle.GetOrSynthesizeAsync(date, clientDate, ct);
            if (entry.Status is not (DayStatuses.Open))
                throw new TrackerException($"Ngày {date} đã đóng sổ — việc của ngày đó không sửa được.");
        }

        return task;
    }

    private static void GuardDate(string date)
    {
        if (!LocalDate.IsValid(date))
            throw new TrackerException($"Ngày '{date}' không đúng dạng yyyy-MM-dd.");
    }
}
