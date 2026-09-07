public static class UiTelemetryHooks
{
    public static void Register()
    {
        EventManager.RegisterClassHandler(
            typeof(ButtonBase),
            ButtonBase.ClickEvent,
            new RoutedEventHandler(OnAnyClick),
            handledEventsToo: true); // still see it even if a handler set e.Handled

        // Also worth hooking, since users don't only click buttons:
        EventManager.RegisterClassHandler(typeof(MenuItem), MenuItem.ClickEvent, new RoutedEventHandler(OnAnyClick), true);
        EventManager.RegisterClassHandler(typeof(Selector), Selector.SelectionChangedEvent, new SelectionChangedEventHandler(OnSelectionChanged), true);
        EventManager.RegisterClassHandler(typeof(TabControl), Selector.SelectionChangedEvent, new SelectionChangedEventHandler(OnTabChanged), true);
    }

    private static void OnAnyClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        var id = ResolveElementId(fe);
        var view = ResolveContainingView(fe);
        Telemetry.Track("ui.click", new Dictionary<string, string>
        {
            ["elementId"] = id,
            ["view"] = view,
            ["elementType"] = fe.GetType().Name
        });
    }

    private static string ResolveElementId(FrameworkElement fe)
    {
        var autoId = AutomationProperties.GetAutomationId(fe);
        if (!string.IsNullOrEmpty(autoId)) return autoId;

        if (!string.IsNullOrEmpty(fe.Name)) return fe.Name;

        // Last resort: type + position in tree — flag these so you notice unnamed elements
        return $"UNNAMED:{fe.GetType().Name}#{fe.GetHashCode()}";
    }

    private static string ResolveContainingView(DependencyObject d)
    {
        // Walk up the visual tree to the nearest UserControl/Window/Page
        while (d != null)
        {
            if (d is FrameworkElement fe && (d is UserControl || d is Window || d is Page))
                return fe.GetType().Name;
            d = VisualTreeHelper.GetParent(d);
        }
        return "Unknown";
    }
}
