namespace DailyTracker.Api.Domain;

/// <summary>
/// Validates a MetricValue against its definition (spec §5): correct slot for the type,
/// min/max, allowed options, maxSelect. On violation → TrackerException, nothing is written.
/// </summary>
public sealed class MetricValidationService
{
    public void Validate(MetricDefinition def, MetricValue value)
    {
        if (def.Key != value.Key)
            throw new TrackerException($"Key mismatch: definition '{def.Key}' but value '{value.Key}'.");

        switch (def.Type)
        {
            case MetricTypes.Scale or MetricTypes.Number:
                RequireSlot(value.Number is not null, def, "number");
                RequireEmpty(value.Text is null && value.Time is null && value.Options is null, def);
                ValidateRange(def, value.Number!.Value);
                break;

            case MetricTypes.Time:
                RequireSlot(value.Time is not null, def, "time");
                RequireEmpty(value.Number is null && value.Text is null && value.Options is null, def);
                if (!TimeOnly.TryParseExact(value.Time, "HH:mm", out _))
                    throw new TrackerException($"'{def.Key}': time must be HH:mm, got '{value.Time}'.");
                break;

            case MetricTypes.Enum:
                RequireSlot(value.Options is { Count: 1 }, def, "options (exactly 1 item)");
                RequireEmpty(value.Number is null && value.Text is null && value.Time is null, def);
                ValidateOptions(def, value.Options!);
                break;

            case MetricTypes.MultiEnum:
                RequireSlot(value.Options is { Count: > 0 }, def, "options");
                RequireEmpty(value.Number is null && value.Text is null && value.Time is null, def);
                ValidateOptions(def, value.Options!);
                if (def.MaxSelect is int max && value.Options!.Count > max)
                    throw new TrackerException($"'{def.Key}': at most {max} options, got {value.Options.Count}.");
                if (value.Options!.Distinct().Count() != value.Options!.Count)
                    throw new TrackerException($"'{def.Key}': duplicate options.");
                break;

            case MetricTypes.Text:
                RequireSlot(value.Text is not null, def, "text");
                RequireEmpty(value.Number is null && value.Time is null && value.Options is null, def);
                break;

            default:
                throw new TrackerException($"'{def.Key}': type '{def.Type}' is not supported.");
        }
    }

    private static void RequireSlot(bool ok, MetricDefinition def, string slot)
    {
        if (!ok) throw new TrackerException($"'{def.Key}' (type {def.Type}): missing or wrong slot {slot}.");
    }

    private static void RequireEmpty(bool ok, MetricDefinition def)
    {
        if (!ok) throw new TrackerException($"'{def.Key}' (type {def.Type}): data in a slot that doesn't belong to this type.");
    }

    private static void ValidateRange(MetricDefinition def, double n)
    {
        var v = def.Validation;
        if (v?.Min is double min && n < min)
            throw new TrackerException($"'{def.Key}': {n} is below min {min}.");
        if (v?.Max is double max && n > max)
            throw new TrackerException($"'{def.Key}': {n} is above max {max}.");
    }

    private static void ValidateOptions(MetricDefinition def, List<string> chosen)
    {
        var allowed = (def.Options ?? []).Select(o => o.Value).ToHashSet();
        var bad = chosen.Where(c => !allowed.Contains(c)).ToList();
        if (bad.Count > 0)
            throw new TrackerException($"'{def.Key}': invalid options: {string.Join(", ", bad)}.");
    }
}
