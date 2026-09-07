using System.Diagnostics;

namespace UiTelemetry;

/// <summary>
/// Decorator around any <see cref="ICommand"/>. This is the precise source of truth: it records
/// user *intent* ("Save was invoked") independently of how it was triggered — button, menu item,
/// keyboard InputBinding, or a call from code. Routed-event hooks cannot see the last two.
/// </summary>
public sealed class TrackedRelayCommand : ITrackedCommand
{
    private readonly ICommand _inner;

    public TrackedRelayCommand(ICommand inner, string commandId)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        CommandId = commandId;
    }

    public string CommandId { get; }

    public bool CanExecute(object? parameter) => _inner.CanExecute(parameter);

    public void Execute(object? parameter)
    {
        var props = new Dictionary<string, string> { ["commandId"] = CommandId };

        // No InteractionScope means nothing in the UI triggered this: a keyboard gesture we do not
        // hook, or code invoking the command directly. Worth distinguishing in the data.
        props["trigger"] = InteractionScope.Current?.Trigger ?? "programmatic";

        var sw = Stopwatch.StartNew();
        try
        {
            _inner.Execute(parameter);
            props["outcome"] = "ok";
        }
        catch (Exception ex)
        {
            props["outcome"] = "faulted";
            props["exception"] = ex.GetType().Name;
            throw;
        }
        finally
        {
            props["durationMs"] = sw.ElapsedMilliseconds.ToString();
            Telemetry.Track("ui.command", props);
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => _inner.CanExecuteChanged += value;
        remove => _inner.CanExecuteChanged -= value;
    }
}
