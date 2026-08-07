using DailyTracker.Api.Domain;
using Xunit;

namespace DailyTracker.Api.Tests;

public class LocalDateTests
{
    // --- Mã tuần ISO 8601, tuần bắt đầu thứ Hai (spec §6) ---

    [Theory]
    [InlineData("2026-08-08", "2026-W32")] // thứ Bảy
    [InlineData("2026-08-03", "2026-W32")] // thứ Hai đầu tuần
    [InlineData("2026-08-09", "2026-W32")] // Chủ nhật cuối tuần
    [InlineData("2026-08-10", "2026-W33")] // thứ Hai tuần kế
    [InlineData("2026-01-01", "2026-W01")]
    [InlineData("2027-01-01", "2026-W53")] // ISO: 1/1/2027 (thứ Sáu) vẫn thuộc tuần 53 của 2026
    [InlineData("2024-12-30", "2025-W01")] // ISO: 30/12/2024 (thứ Hai) đã thuộc tuần 1 của 2025
    public void IsoWeek_theo_chuan_ISO8601(string date, string expected) =>
        Assert.Equal(expected, LocalDate.IsoWeek(date));

    // --- Giờ ngủ qua nửa đêm (spec §8: 23:30 → 07:00 = 7.5) ---

    [Theory]
    [InlineData("23:30", "07:00", 7.5)]
    [InlineData("22:00", "06:00", 8)]
    [InlineData("00:15", "07:45", 7.5)]  // ngủ sau nửa đêm, không qua ngày
    [InlineData("13:00", "14:30", 1.5)]  // ngủ trưa kiểu gì đó — vẫn đúng
    [InlineData("23:00", "23:00", 0)]    // hai mốc trùng nhau = 0, không phải 24
    [InlineData("01:00", "00:30", 23.5)] // wrap gần trọn ngày
    public void SleepHours_xu_ly_qua_nua_dem(string start, string end, double expected) =>
        Assert.Equal(expected, LocalDate.SleepHours(start, end));

    // --- Mặc định dayType theo lịch (spec v3.2) ---

    [Theory]
    [InlineData("2026-08-07", DayTypes.Workday)] // thứ Sáu
    [InlineData("2026-08-08", DayTypes.Weekend)] // thứ Bảy
    [InlineData("2026-08-09", DayTypes.Weekend)] // Chủ nhật
    [InlineData("2026-08-10", DayTypes.Workday)] // thứ Hai
    public void DefaultDayType_T7_CN_la_weekend(string date, string expected) =>
        Assert.Equal(expected, LocalDate.DefaultDayType(date));

    // --- Phép toán chuỗi ngày ---

    [Theory]
    [InlineData("2026-08-08", -1, "2026-08-07")]
    [InlineData("2026-08-31", 1, "2026-09-01")]
    [InlineData("2026-01-01", -1, "2025-12-31")]
    [InlineData("2028-02-28", 1, "2028-02-29")] // năm nhuận
    public void AddDays_qua_bien_thang_nam(string date, int days, string expected) =>
        Assert.Equal(expected, LocalDate.AddDays(date, days));

    [Theory]
    [InlineData("2026-08-08", true)]
    [InlineData("2026-8-8", false)]
    [InlineData("08-08-2026", false)]
    [InlineData("2026-13-01", false)]
    [InlineData("not-a-date", false)]
    public void IsValid_chi_nhan_yyyy_MM_dd(string date, bool expected) =>
        Assert.Equal(expected, LocalDate.IsValid(date));

    [Fact]
    public void MonthKey_lay_7_ky_tu_dau() =>
        Assert.Equal("2026-08", LocalDate.MonthKey("2026-08-08"));
}
