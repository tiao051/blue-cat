using System.Globalization;

namespace DailyTracker.Api.Domain;

/// <summary>
/// Every "date" in the system is a yyyy-MM-dd string in the client's local time (spec §10) —
/// the server does no timezone math. This class gathers all date-string arithmetic.
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

    /// <summary>ISO 8601 week code, Monday-first — e.g. "2026-W32" (spec §6).</summary>
    public static string IsoWeek(string date)
    {
        var d = Parse(date).ToDateTime(TimeOnly.MinValue);
        var week = ISOWeek.GetWeekOfYear(d);
        var year = ISOWeek.GetYear(d);
        return $"{year}-W{week:D2}";
    }

    /// <summary>Month code — e.g. "2026-08".</summary>
    public static string MonthKey(string date) => date[..7];

    /// <summary>Calendar default dayType: Sat/Sun are weekend, the rest workday (spec v3.2).</summary>
    public static string DefaultDayType(string date)
    {
        var dow = Parse(date).DayOfWeek;
        return dow is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayTypes.Weekend : DayTypes.Workday;
    }

    /// <summary>
    /// Sleep hours between two "HH:mm" marks, crossing midnight: 23:30 → 07:00 = 7.5 (spec §8).
    /// start == end means 0 hours of sleep, not 24.
    /// </summary>
    public static double SleepHours(string start, string end)
    {
        var s = TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture);
        var e = TimeOnly.ParseExact(end, "HH:mm", CultureInfo.InvariantCulture);
        var minutes = (e - s).TotalMinutes; // TimeOnly subtraction wraps past midnight, always >= 0
        return Math.Round(minutes / 60.0, 2);
    }
}
