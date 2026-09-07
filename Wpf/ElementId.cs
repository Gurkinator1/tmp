namespace UiTelemetry;

/// <summary>
/// Resolves the semantic name of an element. AutomationProperties.AutomationId is the primary
/// source: it is already the WPF-blessed "stable identity for this control", it is set in XAML
/// next to the thing it names, and UI automation tests can key off the same value.
/// </summary>
public static class ElementId
{
    /// <summary>Prefix for elements nobody named. Alert on these — they are the coverage gap.</summary>
    public const string UnnamedPrefix = "UNNAMED:";

    public static string Resolve(DependencyObject d)
    {
        var autoId = AutomationProperties.GetAutomationId(d);
        if (!string.IsNullOrEmpty(autoId)) return autoId;

        // An explicit opt-in attached property beats a x:Name that only exists for code-behind.
        var tracked = TelemetryProperties.GetTrack(d);
        if (!string.IsNullOrEmpty(tracked)) return tracked!;

        if (d is FrameworkElement { Name.Length: > 0 } fe) return fe.Name;
        if (d is FrameworkContentElement { Name.Length: > 0 } fce) return fce.Name;

        // Do NOT fall back to the visible label: it is localised and it changes.
        // Emit a flagged placeholder instead so unnamed controls show up in a dashboard.
        return $"{UnnamedPrefix}{d.GetType().Name}";
    }

    public static bool IsUnnamed(string id) => id.StartsWith(UnnamedPrefix, StringComparison.Ordinal);
}
