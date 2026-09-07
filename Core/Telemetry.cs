namespace UiTelemetry;

/// <summary>
/// Static entry point. Static on purpose: the WPF class handlers registered by
/// <see cref="UiTelemetryHooks"/> are themselves static and app-lifetime, so there is no
/// instance to inject into them. Everything mutable lives behind <see cref="Configure"/>.
/// </summary>
public static class Telemetry
{
    private static readonly List<ITelemetrySink> Sinks = new();
    private static Func<string?>? _viewResolver;
    private static bool _enabled = true;

    /// <summary>Call once at startup, before the first window is shown.</summary>
    public static void Configure(params ITelemetrySink[] sinks)
    {
        lock (Sinks)
        {
            Sinks.Clear();
            Sinks.AddRange(sinks);
        }
    }

    /// <summary>
    /// Optional override for "which screen is the user on". The hooks can derive the view by
    /// walking the visual tree, but a navigation-driven app usually knows better.
    /// </summary>
    public static void UseViewResolver(Func<string?> resolver) => _viewResolver = resolver;

    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public static void Track(string name, IDictionary<string, string>? properties = null)
    {
        if (!_enabled) return;

        var props = properties is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(properties);

        // Ambient enrichment — every event gets these for free.
        if (!props.ContainsKey("view"))
        {
            var view = _viewResolver?.Invoke() ?? InteractionScope.Current?.View;
            if (!string.IsNullOrEmpty(view)) props["view"] = view!;
        }

        var scope = InteractionScope.Current;
        if (scope is not null)
        {
            props["correlationId"] = scope.CorrelationId;
            if (!props.ContainsKey("sourceElementId") && !props.ContainsKey("elementId") && scope.ElementId is { Length: > 0 })
                props["sourceElementId"] = scope.ElementId;
        }

        var evt = new TelemetryEvent(name, DateTimeOffset.UtcNow, props);

        ITelemetrySink[] sinks;
        lock (Sinks) sinks = Sinks.ToArray();

        foreach (var sink in sinks)
        {
            // Telemetry must never take the app down.
            try { sink.Write(evt); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[telemetry] sink failed: {ex}"); }
        }
    }
}
