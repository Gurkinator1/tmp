using System.Windows.Threading;

namespace UiTelemetry;

/// <summary>
/// Ties together the events produced by one user gesture.
///
/// A single click typically produces two records: the routed-event one ("the user pressed the
/// thing labelled Save") and the command one ("the Save intent actually executed"). Those are
/// different facts and both are worth having — what you must not do is treat them as two
/// interactions. The scope stamps both with the same correlationId so the backend can collapse
/// them, and so a command raised from a keyboard gesture (no click at all) is visibly distinct.
///
/// The UI thread is single-threaded, so a thread-static "current" is sufficient; the scope is
/// torn down once the dispatcher has drained the work the gesture queued.
/// </summary>
public sealed class InteractionScope
{
    [ThreadStatic] private static InteractionScope? _current;

    public static InteractionScope? Current => _current;

    public string CorrelationId { get; }
    public string? ElementId { get; }
    public string? View { get; }
    public string Trigger { get; }

    private InteractionScope(string? elementId, string? view, string trigger)
    {
        CorrelationId = Guid.NewGuid().ToString("n")[..12];
        ElementId = elementId;
        View = view;
        Trigger = trigger;
    }

    /// <summary>Opens a scope for the gesture currently being dispatched. Idempotent within one gesture.</summary>
    public static InteractionScope Begin(string? elementId, string? view, string trigger)
    {
        if (_current is not null) return _current;

        var scope = new InteractionScope(elementId, view, trigger);
        _current = scope;

        // Close it after the input event and everything it synchronously queued has been processed.
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (ReferenceEquals(_current, scope)) _current = null;
        }));

        return scope;
    }
}
