namespace UiTelemetry;

internal static class VisualTreeWalk
{
    /// <summary>
    /// Walks up towards the root, preferring the visual parent but falling back to the logical one.
    /// The fallback matters: menu items, context menus and tooltips live in popups whose visual
    /// parent chain dead-ends before it reaches the owning view, but whose logical chain does not.
    /// </summary>
    public static DependencyObject? Parent(DependencyObject d)
    {
        if (d is Visual or System.Windows.Media.Media3D.Visual3D)
        {
            var visualParent = VisualTreeHelper.GetParent(d);
            if (visualParent is not null) return visualParent;
        }

        return d switch
        {
            FrameworkElement fe => fe.Parent ?? fe.TemplatedParent,
            FrameworkContentElement fce => fce.Parent,
            _ => null
        };
    }

    public static T? FindAncestor<T>(DependencyObject? d) where T : class
    {
        while (d is not null)
        {
            if (d is T match) return match;
            d = Parent(d);
        }
        return null;
    }

    /// <summary>Nearest UserControl / Page / Window above the element — "which screen is this".</summary>
    public static string ResolveContainingView(DependencyObject? d)
    {
        while (d is not null)
        {
            // An explicit ViewName wins — it is inherited, so the first element carrying one
            // answers for its whole subtree.
            var explicitName = TelemetryProperties.GetViewName(d);
            if (!string.IsNullOrEmpty(explicitName)) return explicitName!;

            if (d is UserControl or Page or Window) return d.GetType().Name;
            d = Parent(d);
        }
        return "Unknown";
    }
}
