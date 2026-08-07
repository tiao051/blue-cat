using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using DailyTracker.Api.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Xunit;

namespace DailyTracker.Api.Tests;

/// <summary>
/// Integration test vòng đời ngày (spec §7) — cần Mongo local (docker tracker-mongo, port 27018).
/// Mỗi test một database riêng, seed thật bằng migration, drop khi xong.
/// </summary>
public sealed class DayLifecycleTests : IAsyncLifetime
{
    private const string Uri = "mongodb://localhost:27018";
    private readonly string _dbName = $"tracker_test_{Guid.NewGuid():N}";
    private MongoContext _ctx = null!;
    private DayLifecycleService _svc = null!;

    // Ngày cố định cho test: T5 06/08, T6 07/08, T7 08/08/2026
    private const string D0 = "2026-08-06";
    private const string D1 = "2026-08-07";
    private const string D2 = "2026-08-08";

    public async Task InitializeAsync()
    {
        _ctx = new MongoContext(Uri, _dbName);
        await MigrationRunner.RunAsync(_ctx.Database, NullLogger.Instance);
        _svc = new DayLifecycleService(_ctx, new MetricValidationService());
    }

    public async Task DisposeAsync() => await _ctx.Client.DropDatabaseAsync(_dbName);

    private static List<MetricValue> MorningValues(double? screenTime = null)
    {
        List<MetricValue> values =
        [
            new() { Key = "sleep_start", Time = "23:30" },
            new() { Key = "sleep_end", Time = "07:00" },
            new() { Key = "mood_morning", Number = 7 },
        ];
        if (screenTime is not null)
            values.Add(new MetricValue { Key = "screen_time", Number = screenTime });
        return values;
    }

    private static List<MetricValue> EveningValues() =>
    [
        new() { Key = "productivity", Number = 6 },
        new() { Key = "mood_evening", Number = 7 },
        new() { Key = "physical", Number = 8 },
        new() { Key = "attention_main", Options = ["work", "learning"] },
    ];

    // ---------- Đóng sổ qua check-in sáng hôm sau (spec §7) ----------

    [Fact]
    public async Task Checkin_sang_D2_dong_D1_thanh_closed_khi_du_hai_checkin()
    {
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        await _svc.EveningCheckinAsync(D1, EveningValues());

        await _svc.MorningCheckinAsync(D2, MorningValues(), []);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(DayStatuses.Closed, d1!.Status);
        Assert.NotNull(d1.ClosedAt);
    }

    [Fact]
    public async Task Checkin_sang_D2_dong_D1_thanh_partial_khi_thieu_checkin_toi()
    {
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        // không check-in tối D1

        await _svc.MorningCheckinAsync(D2, MorningValues(), []);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(DayStatuses.Partial, d1!.Status);
    }

    [Fact]
    public async Task Ngay_khong_co_doc_synthesize_thanh_missed()
    {
        var entry = await _svc.GetOrSynthesizeAsync(D0, D2);
        Assert.Equal(DayStatuses.Missed, entry.Status);

        var today = await _svc.GetOrSynthesizeAsync(D2, D2);
        Assert.Equal(DayStatuses.Open, today.Status);
        Assert.Equal(DayTypes.Weekend, today.DayType); // 08/08/2026 là thứ Bảy
    }

    [Fact]
    public async Task EnsureClosedThrough_dong_ngay_cu_theo_du_lieu()
    {
        await _svc.MorningCheckinAsync(D0, MorningValues(), []);
        await _svc.EveningCheckinAsync(D0, EveningValues());

        await _svc.EnsureClosedThroughAsync(D2); // D0 <= D2-2 → đóng

        var d0 = await _svc.GetEntryAsync(D0);
        Assert.Equal(DayStatuses.Closed, d0!.Status);
    }

    [Fact]
    public async Task Doc_chi_co_marker_de_sau_khong_du_lieu_dong_thanh_missed()
    {
        // D1: check-in sáng D2 defer screen_time → tạo marker trên doc D1, nhưng D1 không có gì khác
        await _svc.MorningCheckinAsync(D2, MorningValues(), ["screen_time"]);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.NotNull(d1);
        Assert.Contains("screen_time", d1!.Deferred);

        // 2 ngày sau, D1 bị đóng lazy → missed vì không có dữ liệu thật nào
        await _svc.EnsureClosedThroughAsync("2026-08-10");
        d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(DayStatuses.Missed, d1!.Status);
    }

    // ---------- Chốt mẫu số (spec §6) ----------

    [Fact]
    public async Task QuickPlanned_chot_lan_dau_va_khong_doi()
    {
        await _ctx.Tasks.InsertManyAsync(
        [
            new TaskItem { Title = "a", Category = "personal", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
            new TaskItem { Title = "b", Category = "personal", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
            new TaskItem { Title = "work", Category = "work", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
            new TaskItem { Title = "ongoing", Category = "personal", Kind = "ongoing", Scope = "day", ScopeKey = D2, PlannedDate = D2 },
        ]);

        var entry = await _svc.MorningCheckinAsync(D2, MorningValues(), []);
        Assert.Equal(2, entry.QuickPlanned); // chỉ personal + quick

        // Thêm việc sau check-in sáng rồi check-in lại (sửa trong ngày) — mẫu số không tăng
        await _ctx.Tasks.InsertOneAsync(
            new TaskItem { Title = "c", Category = "personal", Kind = "quick", Scope = "day", ScopeKey = D2, PlannedDate = D2 });
        entry = await _svc.MorningCheckinAsync(D2, MorningValues(), []);
        Assert.Equal(2, entry.QuickPlanned);
    }

    [Fact]
    public async Task Khong_checkin_sang_thi_quickPlanned_null()
    {
        await _svc.EveningCheckinAsync(D2, EveningValues());
        var entry = await _svc.GetEntryAsync(D2);
        Assert.Null(entry!.QuickPlanned);
    }

    // ---------- Ngày sở hữu + để sau (spec §5, §8) ----------

    [Fact]
    public async Task Screen_time_nhap_sang_D2_ghi_vao_doc_D1()
    {
        await _svc.MorningCheckinAsync(D2, MorningValues(screenTime: 4.5), []);

        var d1 = await _svc.GetEntryAsync(D1);
        Assert.NotNull(d1);
        Assert.Equal(4.5, d1!.Values.Single(v => v.Key == "screen_time").Number);

        var d2 = await _svc.GetEntryAsync(D2);
        Assert.DoesNotContain(d2!.Values, v => v.Key == "screen_time");
        Assert.Equal(7, d2.Values.Single(v => v.Key == "mood_morning").Number);
    }

    [Fact]
    public async Task De_sau_hien_dung_ngay_so_huu_va_dien_duoc_trong_han()
    {
        await _svc.MorningCheckinAsync(D2, MorningValues(), ["screen_time"]);

        var deferred = await _svc.GetDeferredAsync(D2);
        var field = Assert.Single(deferred);
        Assert.Equal("screen_time", field.Key);
        Assert.Equal(D1, field.BelongsToDate);      // thuộc về hôm qua
        Assert.Equal(D2, field.LastWritableDate);   // deferrable 1 ngày

        // Điền trong hạn — ghi vào đúng doc D1
        await _svc.SetMetricValueAsync(D1, new MetricValue { Key = "screen_time", Number = 3 }, D2);
        var d1 = await _svc.GetEntryAsync(D1);
        Assert.Equal(3, d1!.Values.Single(v => v.Key == "screen_time").Number);
        Assert.Empty(d1.Deferred);
        Assert.Empty(await _svc.GetDeferredAsync(D2));
    }

    [Fact]
    public async Task Qua_han_de_sau_thi_khong_ghi_duoc_nua()
    {
        await _svc.MorningCheckinAsync(D2, MorningValues(), ["screen_time"]);

        // Sang 10/08 (quá D1 + 1 ngày) — hết hạn
        var ex = await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D1, new MetricValue { Key = "screen_time", Number = 3 }, "2026-08-10"));
        Assert.Contains("quá hạn", ex.Message);

        // Và không còn hiện trong danh sách chờ
        Assert.Empty(await _svc.GetDeferredAsync("2026-08-10"));
    }

    // ---------- Ngày đóng thì khoá (spec §7 v3.2, R18) ----------

    [Fact]
    public async Task Ngay_da_dong_khong_ghi_duoc_metric_thuong_va_habit()
    {
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        await _svc.EveningCheckinAsync(D1, EveningValues());
        await _svc.MorningCheckinAsync(D2, MorningValues(), []); // đóng D1

        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D1, new MetricValue { Key = "mood_evening", Number = 9 }, D2));

        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D1, "gym", HabitStates.Done, null, null));

        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.EveningCheckinAsync(D1, EveningValues()));
    }

    [Fact]
    public async Task Ngay_chua_dong_sua_thoai_mai()
    {
        await _svc.EveningCheckinAsync(D2, EveningValues());
        // 9h tối lỡ vuốt nhầm — sửa lại được vì ngày chưa đóng (spec v3.2)
        await _svc.SetMetricValueAsync(D2, new MetricValue { Key = "mood_evening", Number = 3 }, D2);

        var entry = await _svc.GetEntryAsync(D2);
        Assert.Equal(3, entry!.Values.Single(v => v.Key == "mood_evening").Number);
    }

    // ---------- Habit: 3 trạng thái, hours 0 ≠ no_data, quality gating (spec §6) ----------

    [Fact]
    public async Task Habit_hours_0_la_du_lieu_that()
    {
        await _svc.SetHabitAsync(D2, "reading", HabitStates.NotDone, 0, null);

        var entry = await _svc.GetEntryAsync(D2);
        var reading = entry!.Habits.Single(h => h.HabitKey == "reading");
        Assert.Equal(HabitStates.NotDone, reading.State);
        Assert.Equal(0, reading.Hours); // 0 thật, khác null
    }

    [Fact]
    public async Task Habit_quality_chi_khi_done_va_co_cham_diem()
    {
        // gym có quality, state done → OK
        await _svc.SetHabitAsync(D2, "gym", HabitStates.Done, null, 8);
        var entry = await _svc.GetEntryAsync(D2);
        Assert.Equal(8, entry!.Habits.Single(h => h.HabitKey == "gym").Quality);

        // reading không có quality
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "reading", HabitStates.Done, 1, 8));

        // gym quality nhưng not_done
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "gym", HabitStates.NotDone, null, 5));

        // binary không nhận giờ
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "gym", HabitStates.Done, 1.5, null));

        // no_data không đi kèm giờ
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetHabitAsync(D2, "reading", HabitStates.NoData, 0, null));
    }

    // ---------- Validation (spec §5) ----------

    [Fact]
    public async Task Validation_chan_input_sai_khong_ghi_gi()
    {
        // scale ngoài thang
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "mood_evening", Number = 11 }, D2));

        // multi_enum quá maxSelect 2
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2,
                new MetricValue { Key = "attention_main", Options = ["work", "learning", "phone"] }, D2));

        // option lạ
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "attention_main", Options = ["gaming"] }, D2));

        // key không tồn tại
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "nope", Number = 1 }, D2));

        // sai slot: scale mà đưa text
        await Assert.ThrowsAsync<TrackerException>(() =>
            _svc.SetMetricValueAsync(D2, new MetricValue { Key = "mood_evening", Text = "9" }, D2));

        Assert.Null(await _svc.GetEntryAsync(D2)); // không ghi gì
    }

    [Fact]
    public async Task DayType_doi_duoc_va_mac_dinh_theo_lich()
    {
        var entry = await _svc.SetDayTypeAsync(D2, DayTypes.Dayoff);
        Assert.Equal(DayTypes.Dayoff, entry.DayType);

        // D1 (thứ Sáu) mặc định workday khi doc được tạo qua check-in
        await _svc.MorningCheckinAsync(D1, MorningValues(), []);
        Assert.Equal(DayTypes.Workday, (await _svc.GetEntryAsync(D1))!.DayType);
    }
}
