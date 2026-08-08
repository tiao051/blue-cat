using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using DailyTracker.Api.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DailyTracker.Api.Tests;

/// <summary>Việc vụn + tử số/mẫu số live (spec §6). Cần Mongo local port 27018.</summary>
public sealed class TaskServiceTests : IAsyncLifetime
{
    private const string Uri = "mongodb://localhost:27018";
    private readonly string _dbName = $"tracker_test_{Guid.NewGuid():N}";
    private MongoContext _ctx = null!;
    private DayLifecycleService _lifecycle = null!;
    private TaskService _svc = null!;

    private const string D1 = "2026-08-07";
    private const string D2 = "2026-08-08";

    public async Task InitializeAsync()
    {
        _ctx = new MongoContext(Uri, _dbName);
        await MigrationRunner.RunAsync(_ctx.Database, NullLogger.Instance);
        _lifecycle = new DayLifecycleService(_ctx, new MetricValidationService());
        _svc = new TaskService(_ctx, _lifecycle);
    }

    public async Task DisposeAsync() => await _ctx.Client.DropDatabaseAsync(_dbName);

    private static List<MetricValue> MorningValues() =>
    [
        new() { Key = "sleep_start", Time = "23:30" },
        new() { Key = "sleep_end", Time = "07:00" },
        new() { Key = "mood_morning", Number = 7 },
    ];

    [Fact]
    public async Task Them_viec_tick_done_va_counter_live()
    {
        // 2 việc trước check-in sáng → vào mẫu số
        await _svc.AddAsync("việc a", D2, D2);
        var b = await _svc.AddAsync("việc b", D2, D2);

        var entry = await _lifecycle.MorningCheckinAsync(D2, MorningValues(), []);
        Assert.Equal(2, entry.QuickPlanned);

        // thêm sau khi chốt → addedLater, không tăng mẫu số
        var c = await _svc.AddAsync("việc c", D2, D2);
        await _svc.SetDoneAsync(b.Id.ToString(), true, D2);
        await _svc.SetDoneAsync(c.Id.ToString(), true, D2);

        var (done, addedLater) = await _lifecycle.QuickCountersAsync(
            (await _lifecycle.GetEntryAsync(D2))!);
        Assert.Equal(2, done);       // tỉ lệ 2/2 dù 1 việc là thêm-sau — có thể vượt 1, chủ ý
        Assert.Equal(1, addedLater);
        Assert.Equal(2, (await _lifecycle.GetEntryAsync(D2))!.QuickPlanned); // mẫu số không nhúc nhích
    }

    [Fact]
    public async Task Khong_them_viec_cho_ngay_da_qua_va_viec_ngay_dong_bi_khoa()
    {
        await Assert.ThrowsAsync<TrackerException>(() => _svc.AddAsync("muộn rồi", D1, D2));

        // việc của D1, rồi D1 bị đóng
        var t = await _svc.AddAsync("việc d1", D1, D1);
        await _lifecycle.MorningCheckinAsync(D2, MorningValues(), []); // đóng D1

        await Assert.ThrowsAsync<TrackerException>(() => _svc.SetDoneAsync(t.Id.ToString(), true, D2));
        await Assert.ThrowsAsync<TrackerException>(() => _svc.DropAsync(t.Id.ToString(), D2));
    }

    [Fact]
    public async Task Dong_so_chot_cung_counter_vao_document()
    {
        var t = await _svc.AddAsync("việc d1", D1, D1);
        await _lifecycle.MorningCheckinAsync(D1, MorningValues(), []);
        await _svc.SetDoneAsync(t.Id.ToString(), true, D1);

        await _lifecycle.MorningCheckinAsync(D2, MorningValues(), []); // đóng D1

        var d1 = await _lifecycle.GetEntryAsync(D1);
        Assert.Equal(1, d1!.QuickPlanned);
        Assert.Equal(1, d1.QuickDone); // đã chốt cứng lúc đóng
    }

    [Fact]
    public async Task Drop_viec_khong_vao_counter_va_khong_hien_trong_range()
    {
        var t = await _svc.AddAsync("việc bỏ", D2, D2);
        await _svc.DropAsync(t.Id.ToString(), D2);

        var list = await _svc.GetRangeAsync(D2, D2);
        Assert.Empty(list);
    }
}
