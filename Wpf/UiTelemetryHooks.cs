namespace UiTelemetry;

/// <summary>
/// How to treat a click on a control that also invokes a tracked <see cref="ICommand"/>.
/// </summary>
public enum ClickTrackingPolicy
{
    /// <summary>
    /// Default. The command is the single source of truth for that interaction, so the routed
    /// event is not reported separately — it would double-count. Nothing is lost: the click still
    /// opens the <see cref="InteractionScope"/>, so the command event inherits the element id,
    /// the view and the trigger.
    /// </summary>
    CommandsAreSourceOfTruth,

    /// <summary>Report both. Only useful when you are diffing the two pipelines against each other.</summary>
    TrackEverything
}

/// <summary>
/// App-wide passive coverage via <see cref="EventManager.RegisterClassHandler"/>: one static
/// registration per control class catches every current and future instance, including third-party
/// controls, without touching a single XAML file.
/// </summary>
public static class UiTelemetryHooks
{
    private static bool _registered;

    public static ClickTrackingPolicy Policy { get; set; } = ClickTrackingPolicy.CommandsAreSourceOfTruth;

    /// <summary>
    /// Call from App.OnStartup, before the main window is constructed — class handlers only see
    /// events raised after registration. Guarded: registering twice would attach the handler twice
    /// and duplicate every event (there is no Unregister API).
    /// </summary>
    public static void Register()
    {
        if (_registered) return;
        _registered = true;

        // handledEventsToo: true — a ViewModel or a parent setting e.Handled must not make the
        // interaction invisible to us. We never set e.Handled ourselves; the hook is purely additive.
        EventManager.RegisterClassHandler(typeof(ButtonBase), ButtonBase.ClickEvent,
            new RoutedEventHandler(OnClick), true);

        EventManager.RegisterClassHandler(typeof(MenuItem), MenuItem.ClickEvent,
            new RoutedEventHandler(OnClick), true);

        EventManager.RegisterClassHandler(typeof(System.Windows.Documents.Hyperlink),
            System.Windows.Documents.Hyperlink.ClickEvent, new RoutedEventHandler(OnClick), true);

        // Registered on Selector only. TabControl/ListBox/ComboBox all derive from it, so a second
        // registration on TabControl would fire twice for one tab change.
        EventManager.RegisterClassHandler(typeof(Selector), Selector.SelectionChangedEvent,
            new SelectionChangedEventHandler(OnSelectionChanged), true);

        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded), true);
    }

    private static void OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject d) return;

        var id = ElementId.Resolve(d);
        var view = VisualTreeWalk.ResolveContainingView(d);

        // Open the scope even when we suppress the click event itself: the command event that
        // follows within this dispatcher turn inherits the id/view/correlation from here.
        InteractionScope.Begin(id, view, TriggerOf(e));

        if (Policy == ClickTrackingPolicy.CommandsAreSourceOfTruth && HasTrackedCommand(d))
            return;

        Telemetry.Track("ui.click", new Dictionary<string, string>
        {
            ["elementId"] = id,
            ["view"] = view,
            ["elementType"] = d.GetType().Name,
            // e.OriginalSource is the inner visual actually hit (the icon inside the button's
            // StackPanel content, say) — keep it, it explains odd hit-testing.
            ["originalSource"] = e.OriginalSource?.GetType().Name ?? "?"
        });
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DependencyObject d) return;
        // Selection changes bubble: a ComboBox inside a ListBoxItem would otherwise be attributed
        // to the outer list. Only the control that raised it is interesting.
        if (!ReferenceEquals(sender, e.OriginalSource)) return;

        var id = ElementId.Resolve(d);
        var view = VisualTreeWalk.ResolveContainingView(d);
        InteractionScope.Begin(id, view, "selection");

        Telemetry.Track(d is TabControl ? "ui.tab" : "ui.selection", new Dictionary<string, string>
        {
            ["elementId"] = id,
            ["view"] = view,
            ["elementType"] = d.GetType().Name,
            // The selected value itself is user data — report identity, never content.
            ["selectedId"] = e.AddedItems.Count > 0 ? ElementIdOfItem(e.AddedItems[0]) : "none"
        });
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window w) return;
        Telemetry.Track("ui.view", new Dictionary<string, string>
        {
            ["view"] = w.GetType().Name,
            ["elementType"] = nameof(Window)
        });
    }

    /// <summary>True when the control routes through an <see cref="ITrackedCommand"/> that reports itself.</summary>
    private static bool HasTrackedCommand(DependencyObject d)
        => d is ICommandSource { Command: ITrackedCommand };

    private static string TriggerOf(RoutedEventArgs e)
        => e.OriginalSource is DependencyObject src && Keyboard.FocusedElement == src ? "keyboard" : "pointer";

    private static string ElementIdOfItem(object? item) => item switch
    {
        null => "none",
        DependencyObject d => ElementId.Resolve(d),
        _ => item.GetType().Name
    };
}
