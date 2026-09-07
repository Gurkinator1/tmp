namespace UiTelemetry;

/// <summary>
/// Attached properties for the cases the class handlers cannot infer.
///
/// Note what this deliberately does NOT do: it does not subscribe to Click. Attaching a handler
/// in the property-changed callback would double-count against the class handler and leak a
/// closure per assignment. The class handler already sees the click — the attached property only
/// supplies the name it should use.
/// </summary>
public static class TelemetryProperties
{
    /// <summary>Explicit telemetry id, for elements where AutomationId is unavailable or already taken.</summary>
    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.RegisterAttached(
            "Track", typeof(string), typeof(TelemetryProperties), new PropertyMetadata(null));

    public static void SetTrack(DependencyObject d, string? value) => d.SetValue(TrackProperty, value);
    public static string? GetTrack(DependencyObject d) => (string?)d.GetValue(TrackProperty);

    /// <summary>
    /// Overrides the resolved view name for a subtree. Useful when one UserControl hosts several
    /// logical screens, or when the type name is meaningless (ContentPresenter-driven navigation).
    /// </summary>
    public static readonly DependencyProperty ViewNameProperty =
        DependencyProperty.RegisterAttached(
            "ViewName", typeof(string), typeof(TelemetryProperties),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    public static void SetViewName(DependencyObject d, string? value) => d.SetValue(ViewNameProperty, value);
    public static string? GetViewName(DependencyObject d) => (string?)d.GetValue(ViewNameProperty);
}
