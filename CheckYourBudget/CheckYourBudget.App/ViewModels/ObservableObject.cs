using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CheckYourBudget.App.ViewModels;

/// <summary>Minimal INotifyPropertyChanged base so the UI updates when a property changes.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>A button command that always runs the given action.</summary>
public sealed class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();
}

/// <summary>A button command that runs async work, and ignores taps while it is still running.</summary>
public sealed class AsyncCommand(Func<Task> execute) : ICommand
{
    private bool running;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !running;

    public async void Execute(object? parameter)
    {
        if (running)
        {
            return;
        }

        running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await execute();
        }
        finally
        {
            running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
