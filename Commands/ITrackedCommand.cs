namespace UiTelemetry;

/// <summary>
/// Marker for a command that reports its own execution. The click hook checks for it to decide
/// whether the routed event would be a duplicate.
/// </summary>
public interface ITrackedCommand : ICommand
{
    string CommandId { get; }
}
