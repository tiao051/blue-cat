using DailyTracker.Api.Domain;
using HotChocolate;
using HotChocolate.Execution;

namespace DailyTracker.Api.GraphQL;

/// <summary>TrackerException → GraphQL error sạch; lỗi khác giữ mặc định (che chi tiết ở prod).</summary>
public sealed class TrackerErrorFilter : IErrorFilter
{
    public IError OnError(IError error) =>
        error.Exception is TrackerException tex
            ? error.WithMessage(tex.Message).WithCode("TRACKER_ERROR")
            : error;
}
