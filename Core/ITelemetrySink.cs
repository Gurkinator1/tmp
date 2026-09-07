namespace UiTelemetry;

/// <summary>
/// Where events end up. Swap for App Insights / OTel / a batching HTTP writer.
/// Implementations must be cheap and non-throwing: they run on the UI thread.
/// </summary>
public interface ITelemetrySink
{
    void Write(TelemetryEvent evt);
}

/// <summary>PoC sink — dumps to the debug output window.</summary>
public sealed class DebugTelemetrySink : ITelemetrySink
{
    public void Write(TelemetryEvent evt) => System.Diagnostics.Debug.WriteLine($"[telemetry] {evt}");
}

/// <summary>Keeps the last N events around, handy for a debug overlay or a smoke check.</summary>
public sealed class InMemoryTelemetrySink : ITelemetrySink
{
    private readonly int _capacity;
    private readonly Queue<TelemetryEvent> _events = new();

    public InMemoryTelemetrySink(int capacity = 500) => _capacity = capacity;

    public IReadOnlyCollection<TelemetryEvent> Events
    {
        get { lock (_events) return _events.ToArray(); }
    }

    public void Write(TelemetryEvent evt)
    {
        lock (_events)
        {
            _events.Enqueue(evt);
            while (_events.Count > _capacity) _events.Dequeue();
        }
    }
}
