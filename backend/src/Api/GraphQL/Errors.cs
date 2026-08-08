using DailyTracker.Api.Domain;
using HotChocolate;
using HotChocolate.Execution;

namespace DailyTracker.Api.GraphQL;

/// <summary>TrackerException → clean GraphQL error; other errors keep defaults (details hidden in prod).</summary>
public sealed class TrackerErrorFilter : IErrorFilter
{
    public IError OnError(IError error) =>
        error.Exception is TrackerException tex
            ? error.WithMessage(tex.Message).WithCode("TRACKER_ERROR")
            : error;
}
