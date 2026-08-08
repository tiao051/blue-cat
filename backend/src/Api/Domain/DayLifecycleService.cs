using DailyTracker.Api.Data;
using MongoDB.Driver;

namespace DailyTracker.Api.Domain;

/// <summary>A deferred field still awaiting a value — shown on Today with its owning date (spec §5).</summary>
public sealed record DeferredField(string Key, string Label, string BelongsToDate, string LastWritableDate);

/// <summary>
/// The whole day lifecycle (spec §7) — lives 100% server-side so every client sees the same logic:
/// opening/closing days, closed/partial/missed statuses, denominator locking, deferral windows,
/// closed-day write guards. No cron: closing happens lazily via EnsureClosedThroughAsync on every operation.
/// </summary>
public sealed class DayLifecycleService(MongoContext ctx, MetricValidationService validation)
{
    // ---------- Queries ----------

    public async Task<DailyEntry?> GetEntryAsync(string date, CancellationToken ct = default)
    {
        GuardDate(date);
        return await ctx.DailyEntries.Find(e => e.Date == date).FirstOrDefaultAsync(ct);
    }

    /// <summary>Entry for display: the real doc, or synthesized (missed for the past, default open for today).</summary>
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

    /// <summary>Deferred fields still inside their window and unfilled, for the Today screen.</summary>
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
                if (doc.Values.Any(v => v.Key == key)) continue; // already filled

                var lastWritable = LocalDate.AddDays(doc.Date, def.DeferrableDays.Value);
                if (LocalDate.Compare(clientToday, lastWritable) > 0) continue; // window passed → no_data, hidden

                result.Add(new DeferredField(key, def.Label, doc.Date, lastWritable));
            }
        }

        return result.OrderBy(d => d.BelongsToDate).ThenBy(d => d.Key).ToList();
    }

    // ---------- Lazy closing (spec §7, no cron) ----------

    /// <summary>Closes every open day with date &lt;= clientToday - 2 (D+1 has passed).</summary>
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
        // Freeze the quick counters into the document at close time —
        // open days compute live; closed documents stand alone for Analysis (M4)
        var (done, addedLater) = await QuickCountersAsync(entry, ct);
        entry.QuickDone = done;
        entry.QuickAddedLater = addedLater;

        entry.Status = ResolveClosedStatus(entry);
        entry.ClosedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        await ctx.DailyEntries.ReplaceOneAsync(e => e.Date == entry.Date, entry, cancellationToken: ct);
    }

    /// <summary>Quick numerator + added-after-lock count for a day, computed live from tasks (spec §6).</summary>
    public async Task<(int Done, int AddedLater)> QuickCountersAsync(DailyEntry entry, CancellationToken ct = default)
    {
        var baseFilter = Builders<TaskItem>.Filter.Eq(t => t.Category, "personal")
                         & Builders<TaskItem>.Filter.Eq(t => t.Kind, "quick")
                         & Builders<TaskItem>.Filter.Eq(t => t.PlannedDate, entry.Date)
                         & Builders<TaskItem>.Filter.Ne(t => t.Status, "dropped");

        var done = (int)await ctx.Tasks.CountDocumentsAsync(
            baseFilter & Builders<TaskItem>.Filter.Eq(t => t.Status, "done"), cancellationToken: ct);

        var addedLater = 0;
        if (entry.MorningCheckinAt is DateTime lockAt)
        {
            addedLater = (int)await ctx.Tasks.CountDocumentsAsync(
                baseFilter & Builders<TaskItem>.Filter.Gt(t => t.CreatedAt, lockAt), cancellationToken: ct);
        }

        return (done, addedLater);
    }

    /// <summary>
    /// closed = both of the day's own check-ins; any real data → partial;
    /// a doc holding only deferral markers, no data → missed (spec §7 + principle 3).
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

    // ---------- Morning check-in (spec §7 + §9.1) ----------

    /// <summary>
    /// Day D's morning check-in: writes values to their owning day (dayOffset), records deferrals,
    /// locks the quickPlanned denominator, then closes D-1.
    /// </summary>
    public async Task<DailyEntry> MorningCheckinAsync(
        string date, List<MetricValue> values, List<string> deferredKeys, CancellationToken ct = default)
    {
        GuardDate(date);
        await EnsureClosedThroughAsync(date, ct);

        var defs = await ActiveDefsAsync(ct);
        var now = DateTime.UtcNow;

        // Validate everything before writing anything
        foreach (var v in values)
            validation.Validate(GetDef(defs, v.Key), v);
        foreach (var key in deferredKeys)
        {
            var def = GetDef(defs, key);
            if (def.DeferrableDays is null)
                throw new TrackerException($"'{key}' cannot be deferred (objective data only — spec §5).");
        }

        // Write to the owning day: dayOffset 0 → doc D, -1 → doc D-1 (screen_time)
        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Day {date} is already closed; the check-in can't be edited.");

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

        // Record deferral markers on the owning day's doc
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

        // Lock the denominator — first morning check-in only, it never grows again (spec §6)
        doc.QuickPlanned ??= (int)await ctx.Tasks.CountDocumentsAsync(
            t => t.Category == "personal" && t.Kind == "quick" && t.PlannedDate == date && t.Status != "dropped",
            cancellationToken: ct);

        doc.MorningCheckinAt = now;
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);

        // Close yesterday — after screen_time (if any) has landed in its doc
        var yesterday = await GetEntryAsync(LocalDate.AddDays(date, -1), ct);
        if (yesterday is not null && yesterday.Status == DayStatuses.Open)
            await FinalizeAsync(yesterday, ct);

        return doc;
    }

    // ---------- Evening check-in (spec §9.3) ----------

    public async Task<DailyEntry> EveningCheckinAsync(string date, List<MetricValue> values, CancellationToken ct = default)
    {
        GuardDate(date);
        await EnsureClosedThroughAsync(date, ct);

        var defs = await ActiveDefsAsync(ct);
        foreach (var v in values)
            validation.Validate(GetDef(defs, v.Key), v);

        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Day {date} is already closed; the check-in can't be edited.");

        var now = DateTime.UtcNow;
        foreach (var v in values) WriteValue(doc, v, now);
        doc.EveningCheckinAt = now;
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);
        return doc;
    }

    // ---------- Single-value writes (same-day edits + filling deferred fields) ----------

    /// <summary>
    /// Writes one value to day `date` (the owning day). Open days: edit freely.
    /// Closed days: only deferrable fields still inside their window (spec §5 + v3.2).
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
                throw new TrackerException($"Day {date} is closed — '{value.Key}' can't be backfilled (spec R18).");

            var lastWritable = LocalDate.AddDays(date, def.DeferrableDays.Value);
            if (LocalDate.Compare(clientToday, lastWritable) > 0)
                throw new TrackerException(
                    $"'{value.Key}' for {date} was only writable through {lastWritable} — window passed, it's no_data now.");
        }

        var now = DateTime.UtcNow;
        WriteValue(doc, value, now);
        doc.UpdatedAt = now;
        await UpsertAsync(doc, ct);
        return doc;
    }

    // ---------- Habits (spec §6: 3 states, hours 0 ≠ no_data) ----------

    public async Task<DailyEntry> SetHabitAsync(
        string date, string habitKey, string state, double? hours, int? quality, CancellationToken ct = default)
    {
        GuardDate(date);
        await EnsureClosedThroughAsync(date, ct);

        var habit = await ctx.Habits.Find(h => h.Key == habitKey && h.Active).FirstOrDefaultAsync(ct)
            ?? throw new TrackerException($"Habit '{habitKey}' doesn't exist or is inactive.");

        if (state is not (HabitStates.Done or HabitStates.NotDone or HabitStates.NoData))
            throw new TrackerException($"Invalid state '{state}'.");
        if (habit.Measure == HabitMeasures.Binary && hours is not null)
            throw new TrackerException($"'{habitKey}' is a binary habit; it doesn't take hours.");
        if (hours is < 0 or > 24)
            throw new TrackerException("Hours must be within 0–24.");
        if (quality is not null)
        {
            if (!habit.HasQuality)
                throw new TrackerException($"'{habitKey}' has no quality score.");
            if (state != HabitStates.Done)
                throw new TrackerException("Quality can only be scored when the habit is done (spec §6).");
            if (quality is < 1 or > 10)
                throw new TrackerException("Quality is on a 1–10 scale.");
        }
        if (state == HabitStates.NoData && hours is not null)
            throw new TrackerException("no_data can't carry hours — 0 hours is real data, use done/not_done (principle 7).");

        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Day {date} is closed — habits can't be backfilled (spec R18).");

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
            throw new TrackerException($"Invalid dayType '{dayType}'.");

        await EnsureClosedThroughAsync(date, ct);
        var doc = await GetEntryAsync(date, ct) ?? NewEntry(date);
        if (IsLocked(doc))
            throw new TrackerException($"Day {date} is closed; dayType can't be changed.");

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
        // Writing to the owning day is the deliberate deferrable exception — no IsLocked check here;
        // the window is enforced in SetMetricValueAsync for the fill-later flow; the morning flow is always in-window
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

    /// <summary>closed/partial/missed are immutable to normal mutations (spec §7 v3.2).</summary>
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
            : throw new TrackerException($"No tracked metric '{key}' (or it's inactive).");

    private static void GuardDate(string date)
    {
        if (!LocalDate.IsValid(date))
            throw new TrackerException($"Date '{date}' is not in yyyy-MM-dd format.");
    }
}
