using System.Runtime.CompilerServices;

namespace UiTelemetry;

/// <summary>
/// The DI-friendly interception point: inject this into base ViewModels instead of newing up
/// commands, and every command in the app is instrumented by construction. One place to change,
/// no XAML edits, and an id that cannot silently drift from the member it belongs to.
/// </summary>
public class TrackedCommandFactory
{
    /// <summary>Wraps an existing command (a toolkit RelayCommand, an AsyncRelayCommand, anything).</summary>
    public ITrackedCommand Wrap(ICommand inner, string commandId) => new TrackedRelayCommand(inner, commandId);

    /// <summary>
    /// Ids default to "OwnerType.MemberName" via caller info, so `SaveCommand` on `OrderViewModel`
    /// becomes "OrderViewModel.SaveCommand" without anyone typing a string.
    /// </summary>
    public ITrackedCommand Create(
        object owner,
        Action execute,
        Func<bool>? canExecute = null,
        string? commandId = null,
        [CallerMemberName] string member = "")
        => Wrap(new RelayCommand(execute, canExecute), commandId ?? $"{owner.GetType().Name}.{member}");

    public ITrackedCommand Create(
        object owner,
        Action<object?> execute,
        Func<object?, bool>? canExecute = null,
        string? commandId = null,
        [CallerMemberName] string member = "")
        => Wrap(new RelayCommand(execute, canExecute), commandId ?? $"{owner.GetType().Name}.{member}");
}
