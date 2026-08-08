namespace DailyTracker.Api.Domain;

/// <summary>A domain error whose message is safe to surface directly as a GraphQL error.</summary>
public sealed class TrackerException(string message) : Exception(message);
