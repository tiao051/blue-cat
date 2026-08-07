namespace DailyTracker.Api.Domain;

/// <summary>Lỗi nghiệp vụ có message an toàn để trả thẳng ra GraphQL error.</summary>
public sealed class TrackerException(string message) : Exception(message);
