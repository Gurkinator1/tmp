# tmp

UiTelemetryHooks.cs => WPF click event ButtonBase hook 
App.xaml.cs OnStartup 


AutomationProperties.AutomationId -> seemantischer name für telemetrie
```xaml 
<Button Content="{Binding SaveLabel}"
        AutomationProperties.AutomationId="Toolbar.Save"
        Command="{Binding SaveCommand}" />
```

Next to clicks ICommand.Execute is important for telemetry. 
InputBIndings, gestures, etc?

=> double counting if both are hooked!

command invocation = single source of truth, routed event hook for pure visual/UX interactions that have no command 

base ViewModel/DI-driven factory interception point 

```csharp
public class TrackedCommandFactory
{
    public ICommand Create(Action execute, Func<bool>? canExecute, string id)
        => new TrackedRelayCommand(new RelayCommand(execute, canExecute), id);
}
```


alteernatively; dependecy property. not factory wide, but requires modifying every xaml file.

```csharp
public static class Telemetry
{
    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.RegisterAttached("Track", typeof(string), typeof(Telemetry),
            new PropertyMetadata(null, OnTrackChanged));

    public static void SetTrack(DependencyObject d, string value) => d.SetValue(TrackProperty, value);
    public static string GetTrack(DependencyObject d) => (string)d.GetValue(TrackProperty);

    private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ButtonBase b)
            b.Click += (_, _) => Track("ui.click", new() { ["elementId"] = (string)e.NewValue });
    }
}
```

important information:
automationId, timestamp, current view, correlated command


global RegisterClassHandler for full passive coverage + enforced AutomationId naming convention as the primary approach, with command-factory interception as the more precise long-term source of truth for MVVM intent
