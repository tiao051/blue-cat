namespace DailyTracker.Api.Domain;

/// <summary>
/// Kiểm tra một MetricValue theo definition của nó (spec §5):
/// đúng slot cho type, min/max, options hợp lệ, maxSelect. Sai → TrackerException, không ghi gì.
/// </summary>
public sealed class MetricValidationService
{
    public void Validate(MetricDefinition def, MetricValue value)
    {
        if (def.Key != value.Key)
            throw new TrackerException($"Key không khớp: định nghĩa '{def.Key}' nhưng giá trị '{value.Key}'.");

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
                    throw new TrackerException($"'{def.Key}': time phải có dạng HH:mm, nhận được '{value.Time}'.");
                break;

            case MetricTypes.Enum:
                RequireSlot(value.Options is { Count: 1 }, def, "options (đúng 1 phần tử)");
                RequireEmpty(value.Number is null && value.Text is null && value.Time is null, def);
                ValidateOptions(def, value.Options!);
                break;

            case MetricTypes.MultiEnum:
                RequireSlot(value.Options is { Count: > 0 }, def, "options");
                RequireEmpty(value.Number is null && value.Text is null && value.Time is null, def);
                ValidateOptions(def, value.Options!);
                if (def.MaxSelect is int max && value.Options!.Count > max)
                    throw new TrackerException($"'{def.Key}': chọn tối đa {max} mục, nhận được {value.Options.Count}.");
                if (value.Options!.Distinct().Count() != value.Options!.Count)
                    throw new TrackerException($"'{def.Key}': options bị lặp.");
                break;

            case MetricTypes.Text:
                RequireSlot(value.Text is not null, def, "text");
                RequireEmpty(value.Number is null && value.Time is null && value.Options is null, def);
                break;

            default:
                throw new TrackerException($"'{def.Key}': type '{def.Type}' không được hỗ trợ.");
        }
    }

    private static void RequireSlot(bool ok, MetricDefinition def, string slot)
    {
        if (!ok) throw new TrackerException($"'{def.Key}' (type {def.Type}): thiếu hoặc sai slot {slot}.");
    }

    private static void RequireEmpty(bool ok, MetricDefinition def)
    {
        if (!ok) throw new TrackerException($"'{def.Key}' (type {def.Type}): có dữ liệu ở slot không thuộc type này.");
    }

    private static void ValidateRange(MetricDefinition def, double n)
    {
        var v = def.Validation;
        if (v?.Min is double min && n < min)
            throw new TrackerException($"'{def.Key}': {n} nhỏ hơn min {min}.");
        if (v?.Max is double max && n > max)
            throw new TrackerException($"'{def.Key}': {n} lớn hơn max {max}.");
    }

    private static void ValidateOptions(MetricDefinition def, List<string> chosen)
    {
        var allowed = (def.Options ?? []).Select(o => o.Value).ToHashSet();
        var bad = chosen.Where(c => !allowed.Contains(c)).ToList();
        if (bad.Count > 0)
            throw new TrackerException($"'{def.Key}': option không hợp lệ: {string.Join(", ", bad)}.");
    }
}
