using DailyTracker.Api.Domain;
using Xunit;

namespace DailyTracker.Api.Tests;

public class LocalDateTests
{
    // --- ISO 8601 week codes, Monday-first (spec §6) ---

    [Theory]
    [InlineData("2026-08-08", "2026-W32")] // Saturday
    [InlineData("2026-08-03", "2026-W32")] // Monday, start of week
    [InlineData("2026-08-09", "2026-W32")] // Sunday, end of week
    [InlineData("2026-08-10", "2026-W33")] // next Monday
    [InlineData("2026-01-01", "2026-W01")]
    [InlineData("2027-01-01", "2026-W53")] // ISO: Jan 1 2027 (Friday) still belongs to 2026's week 53
    [InlineData("2024-12-30", "2025-W01")] // ISO: Dec 30 2024 (Monday) already belongs to 2025's week 1
    public void IsoWeek_follows_ISO8601(string date, string expected) =>
        Assert.Equal(expected, LocalDate.IsoWeek(date));

    // --- Sleep across midnight (spec §8: 23:30 → 07:00 = 7.5) ---

    [Theory]
    [InlineData("23:30", "07:00", 7.5)]
    [InlineData("22:00", "06:00", 8)]
    [InlineData("00:15", "07:45", 7.5)]  // fell asleep after midnight, same day
    [InlineData("13:00", "14:30", 1.5)]  // a nap of sorts — still correct
    [InlineData("23:00", "23:00", 0)]    // equal marks = 0 hours, not 24
    [InlineData("01:00", "00:30", 23.5)] // wraps almost a full day
    public void SleepHours_handles_midnight_crossing(string start, string end, double expected) =>
        Assert.Equal(expected, LocalDate.SleepHours(start, end));

    // --- Calendar-default dayType (spec v3.2) ---

    [Theory]
    [InlineData("2026-08-07", DayTypes.Workday)] // Friday
    [InlineData("2026-08-08", DayTypes.Weekend)] // Saturday
    [InlineData("2026-08-09", DayTypes.Weekend)] // Sunday
    [InlineData("2026-08-10", DayTypes.Workday)] // Monday
    public void DefaultDayType_weekend_is_sat_sun(string date, string expected) =>
        Assert.Equal(expected, LocalDate.DefaultDayType(date));

    // --- Date-string arithmetic ---

    [Theory]
    [InlineData("2026-08-08", -1, "2026-08-07")]
    [InlineData("2026-08-31", 1, "2026-09-01")]
    [InlineData("2026-01-01", -1, "2025-12-31")]
    [InlineData("2028-02-28", 1, "2028-02-29")] // leap year
    public void AddDays_crosses_month_and_year(string date, int days, string expected) =>
        Assert.Equal(expected, LocalDate.AddDays(date, days));

    [Theory]
    [InlineData("2026-08-08", true)]
    [InlineData("2026-8-8", false)]
    [InlineData("08-08-2026", false)]
    [InlineData("2026-13-01", false)]
    [InlineData("not-a-date", false)]
    public void IsValid_accepts_only_yyyy_MM_dd(string date, bool expected) =>
        Assert.Equal(expected, LocalDate.IsValid(date));

    [Fact]
    public void MonthKey_takes_first_7_chars() =>
        Assert.Equal("2026-08", LocalDate.MonthKey("2026-08-08"));
}
