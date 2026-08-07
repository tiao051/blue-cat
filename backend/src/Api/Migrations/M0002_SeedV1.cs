using DailyTracker.Api.Data;
using DailyTracker.Api.Domain;
using MongoDB.Driver;

namespace DailyTracker.Api.Migrations;

/// <summary>
/// Seed spec §8: 11 biến + 5 habit + 5 chỉ tiêu tuần khởi điểm + 1 mục tiêu năm.
/// Mọi write là upsert theo khoá tự nhiên (key/title) — chạy lại không nhân đôi (checklist M0).
/// </summary>
public sealed class M0002_SeedV1 : IMigration
{
    public string Id => "M0002_SeedV1";

    private static MetricValidation Scale1To10 => new() { Min = 1, Max = 10, Step = 1, Required = true };

    public async Task UpAsync(IMongoDatabase db, CancellationToken ct)
    {
        // ---- 11 biến theo dõi ----
        MetricDefinition[] metrics =
        [
            // Buổi sáng
            new()
            {
                Key = "sleep_start", Label = "Giờ đi ngủ đêm qua", Type = MetricTypes.Time,
                Phase = Phases.Morning, Order = 10,
                Validation = new MetricValidation { Required = true },
            },
            new()
            {
                Key = "sleep_end", Label = "Giờ thức", Type = MetricTypes.Time,
                Phase = Phases.Morning, Order = 20,
                Validation = new MetricValidation { Required = true },
            },
            new()
            {
                Key = "screen_time", Label = "Screen time (giờ)", Type = MetricTypes.Number,
                Phase = Phases.Morning, Order = 30,
                // Thuộc về hôm qua (dayOffset -1), ghi được tới hết hôm nay (deferrable 1) — spec §5/§8
                DayOffset = -1, DeferrableDays = 1, Polarity = Polarities.HigherWorse,
                Validation = new MetricValidation { Min = 0, Max = 24, Step = 0.5, Required = true },
            },
            new()
            {
                Key = "mood_morning", Label = "Tâm trạng sáng", Type = MetricTypes.Scale,
                Phase = Phases.Morning, Order = 40,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },

            // Buổi tối
            new()
            {
                Key = "productivity", Label = "Hiệu suất", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 10,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                Key = "mood_evening", Label = "Tâm trạng cuối ngày", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 20,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                Key = "physical", Label = "Thể trạng", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 30,
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                // Ngày ốm vẫn hỏi phục hồi (spec v3.2)
                Key = "recovery", Label = "Có thực sự phục hồi không", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 40,
                VisibleWhen = new VisibleWhen { Field = "dayType", Values = [DayTypes.Weekend, DayTypes.Dayoff, DayTypes.Sick] },
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                // Không hỏi ngày ốm (spec v3.2)
                Key = "time_meaningful", Label = "Thời gian rảnh dùng có ý nghĩa không", Type = MetricTypes.Scale,
                Phase = Phases.Evening, Order = 50,
                VisibleWhen = new VisibleWhen { Field = "dayType", Values = [DayTypes.Weekend, DayTypes.Dayoff] },
                Polarity = Polarities.HigherBetter, Validation = Scale1To10,
            },
            new()
            {
                Key = "attention_main", Label = "Tâm trí chủ yếu ở đâu", Type = MetricTypes.MultiEnum,
                Phase = Phases.Evening, Order = 60,
                MaxSelect = 2, // giữ chữ "chủ yếu" — Phụ lục A
                Options =
                [
                    new MetricOption { Value = "work", Label = "Công việc" },
                    new MetricOption { Value = "learning", Label = "Học & phát triển" },
                    new MetricOption { Value = "phone", Label = "Cày phone, giải trí" },
                    new MetricOption { Value = "social", Label = "Xã hội, người khác" },
                    new MetricOption { Value = "empty", Label = "Trống rỗng" },
                ],
                Validation = new MetricValidation { Required = true },
            },
            new()
            {
                Key = "note", Label = "Ghi chú", Type = MetricTypes.Text,
                Phase = Phases.Evening, Order = 70,
                Validation = new MetricValidation { Required = false },
            },
        ];

        var metricCol = db.GetCollection<MetricDefinition>(CollectionNames.MetricDefinitions);
        foreach (var m in metrics)
            await metricCol.ReplaceOneAsync(d => d.Key == m.Key, m, new ReplaceOptions { IsUpsert = true }, ct);

        // ---- 5 habit ----
        Habit[] habits =
        [
            new()
            {
                Key = "gym", Label = "Gym / vận động", ShortLabel = "gym", Icon = "barbell",
                Measure = HabitMeasures.Binary, HasQuality = true, QualityLabel = "Buổi tập có tốt không?", Order = 10,
            },
            new()
            {
                Key = "reading", Label = "Đọc sách", ShortLabel = "đọc", Icon = "book",
                Measure = HabitMeasures.Duration, Order = 20,
            },
            new()
            {
                // Chỉ đếm ngoài giờ làm chính thức (quy ước §8)
                Key = "tech", Label = "Học tech ngoài giờ làm", ShortLabel = "tech", Icon = "code",
                Measure = HabitMeasures.Duration, Order = 30,
            },
            new()
            {
                Key = "rp", Label = "Luyện RP", ShortLabel = "RP", Icon = "microphone",
                Measure = HabitMeasures.Duration, Order = 40,
            },
            new()
            {
                Key = "go_out", Label = "Ra khỏi nhà / gặp người khác", ShortLabel = "ra ngoài", Icon = "door-exit",
                Measure = HabitMeasures.Binary, Order = 50,
            },
        ];

        var habitCol = db.GetCollection<Habit>(CollectionNames.Habits);
        foreach (var h in habits)
            await habitCol.ReplaceOneAsync(d => d.Key == h.Key, h, new ReplaceOptions { IsUpsert = true }, ct);

        // ---- Chỉ tiêu tuần khởi điểm (số liệu đoán hợp lý — user chỉnh sau, bản mới = effectiveFrom mới) ----
        var thisWeek = LocalDate.IsoWeek(LocalDate.ToDateString(DateOnly.FromDateTime(DateTime.Now)));
        (string HabitKey, double Target, string Unit)[] targets =
        [
            ("gym", 3, "sessions"),
            ("reading", 3, "hours"),
            ("tech", 3, "hours"),
            ("rp", 2, "hours"),
            ("go_out", 2, "sessions"),
        ];

        var targetCol = db.GetCollection<HabitTarget>(CollectionNames.HabitTargets);
        foreach (var (habitKey, target, unit) in targets)
        {
            var doc = new HabitTarget { HabitKey = habitKey, Target = target, Unit = unit, EffectiveFrom = thisWeek };
            // Upsert theo habitKey: seed chỉ đảm bảo mỗi habit có 1 chỉ tiêu khởi điểm
            await targetCol.ReplaceOneAsync(t => t.HabitKey == habitKey, doc, new ReplaceOptions { IsUpsert = true }, ct);
        }

        // ---- 1 mục tiêu năm (v1: read-only, một dòng nhắc — R12) ----
        var goal = new Goal { Title = "Du học", Scope = "year" };
        await db.GetCollection<Goal>(CollectionNames.Goals)
            .ReplaceOneAsync(g => g.Title == goal.Title && g.Scope == "year", goal, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
