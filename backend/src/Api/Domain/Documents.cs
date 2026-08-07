using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DailyTracker.Api.Domain;

// Các giá trị union lưu dạng string đúng như spec (scale, not_done, multi_enum...)
// thay vì enum C# — BSON đọc/ghi khớp nguyên văn spec §6, GraphQL layer map enum sau (M1).
public static class MetricTypes
{
    public const string Scale = "scale";
    public const string Number = "number";
    public const string Time = "time";
    public const string Enum = "enum";
    public const string MultiEnum = "multi_enum";
    public const string Text = "text";
}

public static class Phases
{
    public const string Morning = "morning";
    public const string Evening = "evening";
    public const string Anytime = "anytime";
}

public static class DayTypes
{
    public const string Workday = "workday";
    public const string Weekend = "weekend";
    public const string Dayoff = "dayoff";
    public const string Sick = "sick";
}

public static class DayStatuses
{
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Partial = "partial";
    public const string Missed = "missed";
}

public static class HabitStates
{
    public const string Done = "done";
    public const string NotDone = "not_done";
    public const string NoData = "no_data";
}

public static class Polarities
{
    public const string HigherBetter = "higher_better";
    public const string HigherWorse = "higher_worse";
}

/// <summary>Spec §5. Một biến theo dõi — form dựng từ đây, không hardcode.</summary>
public sealed class MetricDefinition
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    /// <summary>Định danh, không bao giờ đổi ý nghĩa (quy tắc versioning §5).</summary>
    public required string Key { get; set; }

    public required string Label { get; set; }
    public required string Type { get; set; }
    public required string Phase { get; set; }
    public int Order { get; set; }

    /// <summary>Điều kiện hiện — phép khớp giá trị đơn giản, không DSL (§5).</summary>
    public VisibleWhen? VisibleWhen { get; set; }

    /// <summary>Số ngày còn ghi được sau ngày sở hữu. Chỉ cho dữ liệu khách quan (§5).</summary>
    public int? DeferrableDays { get; set; }

    /// <summary>
    /// Giá trị nhập ở check-in ngày D ghi vào document ngày D + dayOffset.
    /// screen_time: -1 (thuộc về hôm qua). Addendum ngoài bảng §5 — xem README.
    /// </summary>
    public int DayOffset { get; set; }

    public string? Polarity { get; set; }
    public MetricValidation? Validation { get; set; }
    public List<MetricOption>? Options { get; set; }
    public int? MaxSelect { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class VisibleWhen
{
    public required string Field { get; set; }
    public required List<string> Values { get; set; }
}

public sealed class MetricValidation
{
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Step { get; set; }
    public bool? Required { get; set; }
}

/// <summary>Option của enum/multi_enum: value bất biến (mang ý nghĩa), label đổi thoải mái.</summary>
public sealed class MetricOption
{
    public required string Value { get; set; }
    public required string Label { get; set; }
}

/// <summary>Spec §6 `habits`.</summary>
public sealed class Habit
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    /// <summary>Định danh bất biến, tham chiếu từ daily_entries và habit_targets.</summary>
    public required string Key { get; set; }

    public required string Label { get; set; }

    /// <summary>Tối đa ~8 ký tự, chỉ dùng trong ô lưới.</summary>
    public required string ShortLabel { get; set; }

    public required string Icon { get; set; }

    /// <summary>binary hoặc duration.</summary>
    public required string Measure { get; set; }

    public bool HasQuality { get; set; }
    public string? QualityLabel { get; set; }
    public bool Active { get; set; } = true;
    public int Order { get; set; }
}

public static class HabitMeasures
{
    public const string Binary = "binary";
    public const string Duration = "duration";
}

/// <summary>Spec §6 `habit_targets`. Đổi chỉ tiêu = bản ghi mới với effectiveFrom mới, không sửa cũ.</summary>
public sealed class HabitTarget
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public required string HabitKey { get; set; }

    /// <summary>Chỉ nhận "week" (§6).</summary>
    public string Period { get; set; } = "week";

    public double Target { get; set; }

    /// <summary>sessions hoặc hours.</summary>
    public required string Unit { get; set; }

    /// <summary>Mã tuần ISO đầu tiên áp dụng, vd "2026-W32".</summary>
    public required string EffectiveFrom { get; set; }
}

/// <summary>Spec §6 `daily_entries` — một document một ngày, date là khoá duy nhất.</summary>
public sealed class DailyEntry
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    /// <summary>yyyy-MM-dd theo giờ local (§10) — unique index.</summary>
    public required string Date { get; set; }

    public string Status { get; set; } = DayStatuses.Open;
    public string DayType { get; set; } = DayTypes.Workday;

    /// <summary>Túi key-value động — key lấy từ metric_definitions.</summary>
    public List<MetricValue> Values { get; set; } = [];

    public List<HabitEntry> Habits { get; set; } = [];

    /// <summary>Mẫu số — chốt lúc check-in sáng kết thúc, không tăng nữa. null = no_data (không check-in sáng).</summary>
    public int? QuickPlanned { get; set; }

    public int QuickDone { get; set; }
    public int QuickAddedLater { get; set; }
    public int OngoingTouched { get; set; }

    public DateTime? MorningCheckinAt { get; set; }
    public DateTime? EveningCheckinAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Mốc cập nhật theo từng trường — last-write-wins khi sync (§10).</summary>
    public Dictionary<string, DateTime> FieldUpdatedAt { get; set; } = [];
}

/// <summary>
/// Một giá trị metric: slot đúng kiểu được điền, còn lại null (bị bỏ khi serialize).
/// Mongo lưu đúng shape GraphQL trả về — spec §10 "mỗi cặp có sẵn ô cho từng kiểu dữ liệu".
/// </summary>
public sealed class MetricValue
{
    public required string Key { get; set; }
    public double? Number { get; set; }
    public string? Text { get; set; }

    /// <summary>"HH:mm" cho type time.</summary>
    public string? Time { get; set; }

    public List<string>? Options { get; set; }
}

/// <summary>state luôn 3 giá trị kể cả habit đo giờ; hours 0 là dữ liệu thật ≠ no_data (§6).</summary>
public sealed class HabitEntry
{
    public required string HabitKey { get; set; }
    public string State { get; set; } = HabitStates.NoData;
    public double? Hours { get; set; }

    /// <summary>Chỉ tồn tại khi habit có chấm điểm và state là done.</summary>
    public int? Quality { get; set; }
}

/// <summary>Spec §6 `tasks`. v1 chỉ scope day/week.</summary>
public sealed class TaskItem
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public required string Title { get; set; }

    /// <summary>personal hoặc work — work không vào phân tích (§6).</summary>
    public required string Category { get; set; }

    /// <summary>quick hoặc ongoing.</summary>
    public required string Kind { get; set; }

    /// <summary>day · week (month để v2).</summary>
    public required string Scope { get; set; }

    /// <summary>Ngày cụ thể hoặc mã tuần ISO.</summary>
    public required string ScopeKey { get; set; }

    /// <summary>Chỉ có khi scope là day.</summary>
    public string? PlannedDate { get; set; }

    public string Status { get; set; } = "todo";
    public string? OriginalDate { get; set; }
    public int CarryCount { get; set; }
    public List<string> TouchedDates { get; set; } = [];
    public DateTime? DoneAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Spec §6 `goals`. v1: seed 1 document năm, read-only.</summary>
public sealed class Goal
{
    [BsonId, BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public required string Title { get; set; }

    /// <summary>year hoặc month.</summary>
    public required string Scope { get; set; }

    public string? TargetDate { get; set; }
    public ObjectId? ParentId { get; set; }
    public string Status { get; set; } = "active";
    public bool Active { get; set; } = true;
}
