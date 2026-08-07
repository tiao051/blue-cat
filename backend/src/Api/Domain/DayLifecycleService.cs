using DailyTracker.Api.Data;
using MongoDB.Driver;

namespace DailyTracker.Api.Domain;

/// <summary>Field "để sau" đang chờ điền — trả về cho màn Hôm nay kèm ngày nó thuộc về (spec §5).</summary>
public sealed record DeferredField(string Key, string Label, string BelongsToDate, string LastWritableDate);

/// <summary>
/// Toàn bộ vòng đời một ngày (spec §7) — nằm 100% server để mọi client thấy cùng logic:
/// mở/đóng sổ, trạng thái closed/partial/missed, chốt mẫu số, hạn "để sau", guard ngày đã đóng.
/// Không có cron: đóng sổ lazy qua EnsureClosedThroughAsync trên mọi thao tác.
/// </summary>
public sealed class DayLifecycleService(MongoContext ctx, MetricValidationService validation)
{
    // ---------- Queries ----------

    public async Task<DailyEntry?> GetEntryAsync(string date, CancellationToken ct = default)
    {
        GuardDate(date);
        return await ctx.DailyEntries.Find(e => e.Date == date).FirstOrDefaultAsync(ct);
    }

    /// <summary>Entry cho màn hình: doc thật, hoặc synthesize (missed cho quá khứ, open mặc định cho hôm nay).</summary>
    public async Task<DailyEntry> GetOrSynthesizeAsync(string date, string clientToday, CancellationToken ct = default)
    {
        GuardDate(date);
        GuardDate(clientToday);
        var entry = await GetEntryAsync(date, ct);
        if (entry is not null) return entry;

        return new DailyEntry
        {
            Date = date,
            Status = LocalDate.Compare(date, clientToday) < 0 ? DayStatuses.Missed : DayStatuses.Open,
            DayType = LocalDate.DefaultDayType(date),
        };
    }

    /// <summary>Các field để-sau còn trong hạn và chưa điền, cho màn Hôm nay.</summary>
    public async Task<List<DeferredField>> GetDeferredAsync(string clientToday, CancellationToken ct = default)
    {
        GuardDate(clientToday);
        var defs = await ActiveDefsAsync(ct);
        var deferrable = defs.Values.Where(d => d.DeferrableDays is not null).ToList();
        if (deferrable.Count == 0) return [];

        var maxWindow = deferrable.Max(d => d.DeferrableDays!.Value);
        var from = LocalDate.AddDays(clientToday, -maxWindow);
        var docFilter = Builders<DailyEntry>.Filter.Gte(e => e.Date, from)
                        & Builders<DailyEntry>.Filter.Lte(e => e.Date, clientToday)
                        & Builders<DailyEntry>.Filter.Exists(e => e.Deferred)
                        & Builders<DailyEntry>.Filter.Ne("deferred", Array.Empty<string>());
        var docs = await ctx.DailyEntries.Find(docFilter).ToListAsync(ct);

        var result = new List<DeferredField>();
        foreach (var doc in docs)
        {
            foreach (var key in doc.Deferred)
            {
                if (!defs.TryGetValue(key, out var def) || def.DeferrableDays is null) continue;
                if (doc.Values.Any(v => v.Key == key)) continue; // đã điền rồi

                var lastWritable = LocalDate.AddDays(doc.Date, def.DeferrableDays.Value);
                if (LocalDate.Compare(clientToday, lastWritable) > 0) continue; // quá hạn → no_data, không hiện

                result.Add(new DeferredField(key, def.Label, doc.Date, lastWritable));
            }
        }

        return result.OrderBy(d => d.BelongsToDate).ThenBy(d => d.Key).ToList();
    }

    // ---------- Đóng sổ lazy (spec §7, không cron) ----------

    /// <summary>Đóng mọi ngày open có date &lt;= clientToday - 2 (D+1 đã trôi qua).</summary>
    public async Task EnsureClosedThroughAsync(string clientToday, CancellationToken ct = default)
    {
        GuardDate(clientToday);
        var cutoff = LocalDate.AddDays(clientToday, -2);
        var staleFilter = Builders<DailyEntry>.Filter.Eq(e => e.Status, DayStatuses.Open)
                          & Builders<DailyEntry>.Filter.Lte(e => e.Date, cutoff);
        var stale = await ctx.DailyEntries.Find(staleFilter).ToListAsync(ct);

        foreach (var entry in stale)
            await FinalizeAsync(entry, ct);
    }

    private async Task FinalizeAsync(DailyEntry entry, CancellationToken ct)
    {
        entry.Status = ResolveClosedStatus(entry);
        entry.ClosedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        await ctx.DailyEntries.ReplaceOneAsync(e => e.Date == entry.Date, entry, cancellationToken: ct);
    }

    /// <summary>
    /// closed = đủ cả hai check-in của chính ngày đó; có bất kỳ dữ liệu thật nào → partial;
    /// doc chỉ có marker để-sau, không dữ liệu → missed (spec §7 + nguyên tắc 3).
    /// </summary>
    private static string ResolveClosedStatus(DailyEntry e)
    {
        if (e.MorningCheckinAt is not null && e.EveningCheckinAt is not null) return DayStatuses.Closed;
        if (HasAnyData(e)) return DayStatuses.Partial;
        return DayStatuses.Missed;
    }

    private static bool HasAnyData(DailyEntry e) =>
        e.MorningCheckinAt is not null
        || e.EveningCheckinAt is not null
        || e.Values.Count > 0
        || e.Habits.Any(h => h.State != HabitStates.NoData || h.Hours is not null);

    // ---------- Check-in sáng (spec §7 + §9.1) ----------

    /// <summary>
    /// Check-in sáng của ngày D: ghi values vào đúng ngày sở hữu (dayOffset), đánh dấu để-sau,
    /// chốt mẫu số quickPlanned, rồi đóng sổ D-1.
    /// </summary>
    public async Task<DailyEntry> MorningCheckinAsync(
        string date, List<MetricValue> values, List<string> deferredKeys, CancellationToken ct = default)
    {
        GuardDate(date);
        await EnsureClosedThroughAsync(date, ct);

        var defs = await ActiveDefsAsync(ct);
        var now = DateTime.UtcNow;

        // Validate tất cả trước khi ghi bất cứ gì
        foreach (var v in values)
            validation.Validate(GetDef(defs, v.Key), v);
        foreach (var key in deferredKeys)
        {
            var def = GetDef(defs, key);
            if (def.DeferrableDays is null)
                throw new TrackerException($"'{key}' không phải field để-sau được (chỉ dữ liệu khách quan — spec §5).");
        }

        // Ghi theo ngày sở hữu: dayOffset 0 → doc D, -1 → doc D-1 (screen_time)
        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Ngày {date} đã đóng sổ, không sửa được check-in.");

        foreach (var group in values.GroupBy(v => defs[v.Key].DayOffset))
        {
            if (group.Key == 0)
            {
                foreach (var v in group) WriteValue(doc, v, now);
            }
            else
            {
                var targetDate = LocalDate.AddDays(date, group.Key);
                await WriteToOwningDayAsync(targetDate, [.. group], deferredMarkers: [], now, ct);
            }
        }

        // Đánh dấu để-sau vào doc của ngày sở hữu
        foreach (var group in deferredKeys.GroupBy(k => defs[k].DayOffset))
        {
            var targetDate = LocalDate.AddDays(date, group.Key);
            if (group.Key == 0)
            {
                foreach (var k in group)
                    if (!doc.Deferred.Contains(k) && !doc.Values.Any(v => v.Key == k))
                        doc.Deferred.Add(k);
            }
            else
            {
                await WriteToOwningDayAsync(targetDate, [], [.. group], now, ct);
            }
        }

        // Chốt mẫu số — chỉ lần check-in sáng đầu tiên, không bao giờ tăng lại (spec §6)
        doc.QuickPlanned ??= (int)await ctx.Tasks.CountDocumentsAsync(
            t => t.Category == "personal" && t.Kind == "quick" && t.PlannedDate == date && t.Status != "dropped",
            cancellationToken: ct);

        doc.MorningCheckinAt = now;
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);

        // Đóng sổ hôm qua — sau khi screen_time (nếu có) đã vào doc của nó
        var yesterday = await GetEntryAsync(LocalDate.AddDays(date, -1), ct);
        if (yesterday is not null && yesterday.Status == DayStatuses.Open)
            await FinalizeAsync(yesterday, ct);

        return doc;
    }

    // ---------- Check-in tối (spec §9.3) ----------

    public async Task<DailyEntry> EveningCheckinAsync(string date, List<MetricValue> values, CancellationToken ct = default)
    {
        GuardDate(date);
        await EnsureClosedThroughAsync(date, ct);

        var defs = await ActiveDefsAsync(ct);
        foreach (var v in values)
            validation.Validate(GetDef(defs, v.Key), v);

        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Ngày {date} đã đóng sổ, không sửa được check-in.");

        var now = DateTime.UtcNow;
        foreach (var v in values) WriteValue(doc, v, now);
        doc.EveningCheckinAt = now;
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);
        return doc;
    }

    // ---------- Ghi lẻ một giá trị (sửa trong ngày + điền field để-sau) ----------

    /// <summary>
    /// Ghi một giá trị vào ngày `date` (ngày sở hữu). Ngày chưa đóng: sửa thoải mái.
    /// Ngày đã đóng: chỉ field deferrable còn trong hạn (spec §5 + v3.2).
    /// </summary>
    public async Task<DailyEntry> SetMetricValueAsync(
        string date, MetricValue value, string clientToday, CancellationToken ct = default)
    {
        GuardDate(date);
        GuardDate(clientToday);
        await EnsureClosedThroughAsync(clientToday, ct);

        var defs = await ActiveDefsAsync(ct);
        var def = GetDef(defs, value.Key);
        validation.Validate(def, value);

        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        var isPastClosed = IsLocked(doc) || (doc.Id == default && LocalDate.Compare(date, clientToday) < 0);

        if (isPastClosed)
        {
            if (def.DeferrableDays is null)
                throw new TrackerException($"Ngày {date} đã đóng sổ — '{value.Key}' không ghi bù được (spec R18).");

            var lastWritable = LocalDate.AddDays(date, def.DeferrableDays.Value);
            if (LocalDate.Compare(clientToday, lastWritable) > 0)
                throw new TrackerException(
                    $"'{value.Key}' của ngày {date} chỉ ghi được tới hết {lastWritable} — đã quá hạn, thành no_data.");
        }

        var now = DateTime.UtcNow;
        WriteValue(doc, value, now);
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);
        return doc;
    }

    // ---------- Habit (spec §6: 3 trạng thái, hours 0 ≠ no_data) ----------

    public async Task<DailyEntry> SetHabitAsync(
        string date, string habitKey, string state, double? hours, int? quality, CancellationToken ct = default)
    {
        GuardDate(date);
        await EnsureClosedThroughAsync(date, ct);

        var habit = await ctx.Habits.Find(h => h.Key == habitKey && h.Active).FirstOrDefaultAsync(ct)
            ?? throw new TrackerException($"Habit '{habitKey}' không tồn tại hoặc đã tắt.");

        if (state is not (HabitStates.Done or HabitStates.NotDone or HabitStates.NoData))
            throw new TrackerException($"State '{state}' không hợp lệ.");
        if (habit.Measure == HabitMeasures.Binary && hours is not null)
            throw new TrackerException($"'{habitKey}' là habit binary, không nhận số giờ.");
        if (hours is < 0 or > 24)
            throw new TrackerException($"Số giờ phải trong 0–24.");
        if (quality is not null)
        {
            if (!habit.HasQuality)
                throw new TrackerException($"'{habitKey}' không có chấm điểm chất lượng.");
            if (state != HabitStates.Done)
                throw new TrackerException("Chỉ chấm điểm khi habit đã done (spec §6).");
            if (quality is < 1 or > 10)
                throw new TrackerException("Điểm chất lượng trong thang 1–10.");
        }
        if (state == HabitStates.NoData && hours is not null)
            throw new TrackerException("no_data không đi kèm số giờ — 0 giờ là dữ liệu thật, hãy dùng done/not_done (nguyên tắc 7).");

        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Ngày {date} đã đóng sổ — habit không ghi bù được (spec R18).");

        var now = DateTime.UtcNow;
        var entry = doc.Habits.FirstOrDefault(h => h.HabitKey == habitKey);
        if (entry is null)
        {
            entry = new HabitEntry { HabitKey = habitKey };
            doc.Habits.Add(entry);
        }

        entry.State = state;
        entry.Hours = hours;
        entry.Quality = state == HabitStates.Done ? quality : null;
        doc.FieldUpdatedAt[$"habit:{habitKey}"] = now;
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);
        return doc;
    }

    // ---------- dayType ----------

    public async Task<DailyEntry> SetDayTypeAsync(string date, string dayType, CancellationToken ct = default)
    {
        GuardDate(date);
        if (dayType is not (DayTypes.Workday or DayTypes.Weekend or DayTypes.Dayoff or DayTypes.Sick))
            throw new TrackerException($"dayType '{dayType}' không hợp lệ.");

        await EnsureClosedThroughAsync(date, ct);
        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Ngày {date} đã đóng sổ, không đổi dayType được.");

        doc.DayType = dayType;
        doc.UpdatedAt = DateTime.UtcNow;
        await UpsertAsync(doc, ct);
        return doc;
    }

    // ---------- Helpers ----------

    private async Task WriteToOwningDayAsync(
        string targetDate, List<MetricValue> values, List<string> deferredMarkers, DateTime now, CancellationToken ct)
    {
        var doc = await GetEntryAsync(targetDate, ct) ?? NewEntry(targetDate);
        // Ghi vào ngày sở hữu là ngoại lệ deferrable có chủ đích — không check IsLocked ở đây,
        // hạn được enforce ở SetMetricValueAsync cho luồng điền-sau; luồng check-in sáng luôn trong hạn
        foreach (var v in values) WriteValue(doc, v, now);
        foreach (var k in deferredMarkers)
            if (!doc.Deferred.Contains(k) && !doc.Values.Any(v => v.Key == k))
                doc.Deferred.Add(k);
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);
    }

    private static void WriteValue(DailyEntry doc, MetricValue value, DateTime now)
    {
        doc.Values.RemoveAll(v => v.Key == value.Key);
        doc.Values.Add(value);
        doc.Deferred.Remove(value.Key);
        doc.FieldUpdatedAt[value.Key] = now;
    }

    private static DailyEntry NewEntry(string date) => new()
    {
        Date = date,
        Status = DayStatuses.Open,
        DayType = LocalDate.DefaultDayType(date),
    };

    /// <summary>closed và missed là bất biến với mutation thường (spec §7 v3.2).</summary>
    private static bool IsLocked(DailyEntry doc) =>
        doc.Status is DayStatuses.Closed or DayStatuses.Partial or DayStatuses.Missed;

    private async Task UpsertAsync(DailyEntry doc, CancellationToken ct) =>
        await ctx.DailyEntries.ReplaceOneAsync(
            e => e.Date == doc.Date, doc, new ReplaceOptions { IsUpsert = true }, ct);

    private async Task<Dictionary<string, MetricDefinition>> ActiveDefsAsync(CancellationToken ct) =>
        (await ctx.MetricDefinitions.Find(d => d.Active).ToListAsync(ct)).ToDictionary(d => d.Key);

    private static MetricDefinition GetDef(Dictionary<string, MetricDefinition> defs, string key) =>
        defs.TryGetValue(key, out var def)
            ? def
            : throw new TrackerException($"Không có biến theo dõi '{key}' (hoặc đã tắt).");

    private static void GuardDate(string date)
    {
        if (!LocalDate.IsValid(date))
            throw new TrackerException($"Ngày '{date}' không đúng dạng yyyy-MM-dd.");
    }
}
