using System.Globalization;

namespace DailyTracker.Api.Domain;

/// <summary>
/// Mọi "ngày" trong hệ thống là chuỗi yyyy-MM-dd theo giờ local của client (spec §10) —
/// server không làm timezone math. Class này gom toàn bộ phép toán trên chuỗi ngày.
/// </summary>
public static class LocalDate
{
    private const string Format = "yyyy-MM-dd";

    public static bool IsValid(string date) =>
        DateOnly.TryParseExact(date, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public static DateOnly Parse(string date) =>
        DateOnly.ParseExact(date, Format, CultureInfo.InvariantCulture);

    public static string ToDateString(DateOnly d) => d.ToString(Format, CultureInfo.InvariantCulture);

    public static string AddDays(string date, int days) => ToDateString(Parse(date).AddDays(days));

    public static int Compare(string a, string b) => string.CompareOrdinal(a, b);

    /// <summary>Mã tuần ISO 8601, tuần bắt đầu thứ Hai — vd "2026-W32" (spec §6).</summary>
    public static string IsoWeek(string date)
    {
        var d = Parse(date).ToDateTime(TimeOnly.MinValue);
        var week = ISOWeek.GetWeekOfYear(d);
        var year = ISOWeek.GetYear(d);
        return $"{year}-W{week:D2}";
    }

    /// <summary>Mã tháng — vd "2026-08".</summary>
    public static string MonthKey(string date) => date[..7];

    /// <summary>Mặc định dayType theo lịch: T7/CN là weekend, còn lại workday (spec v3.2).</summary>
    public static string DefaultDayType(string date)
    {
        var dow = Parse(date).DayOfWeek;
        return dow is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayTypes.Weekend : DayTypes.Workday;
    }

    /// <summary>
    /// Tổng giờ ngủ từ hai mốc "HH:mm", xử lý qua nửa đêm: 23:30 → 07:00 = 7.5 (spec §8).
    /// start == end hiểu là ngủ 0 tiếng, không phải 24.
    /// </summary>
    public static double SleepHours(string start, string end)
    {
        var s = TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture);
        var e = TimeOnly.ParseExact(end, "HH:mm", CultureInfo.InvariantCulture);
        var minutes = (e - s).TotalMinutes; // TimeOnly trừ nhau tự wrap qua nửa đêm, kết quả luôn >= 0
        return Math.Round(minutes / 60.0, 2);
    }
}
