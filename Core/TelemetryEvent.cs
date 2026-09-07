namespace UiTelemetry;

/// <summary>
/// A single interaction record. Deliberately flat and stringly-typed: it is what
/// almost every analytics backend (App Insights, Segment, OTel attributes) wants anyway.
/// </summary>
public sealed record TelemetryEvent(
    string Name,
    DateTimeOffset TimestampUtc,
    IReadOnlyDictionary<string, string> Properties)
{
    public string? Get(string key) => Properties.TryGetValue(key, out var v) ? v : null;

    public override string ToString()
    {
        var props = string.Join(", ", Properties.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{TimestampUtc:HH:mm:ss.fff} {Name} {{ {props} }}";
    }
}
