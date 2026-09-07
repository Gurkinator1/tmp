public sealed class TrackedRelayCommand : ICommand
{
    private readonly ICommand _inner;
    private readonly string _id;

    public TrackedRelayCommand(ICommand inner, string id)
    {
        _inner = inner;
        _id = id;
    }

    public bool CanExecute(object? parameter) => _inner.CanExecute(parameter);

    public void Execute(object? parameter)
    {
        Telemetry.Track("ui.command", new Dictionary<string, string> { ["commandId"] = _id });
        _inner.Execute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => _inner.CanExecuteChanged += value;
        remove => _inner.CanExecuteChanged -= value;
    }
}
