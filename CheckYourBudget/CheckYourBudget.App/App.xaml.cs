using CheckYourBudget.App.ViewModels;

namespace CheckYourBudget.App;

public partial class App : Application
{
    private readonly IServiceProvider services;

    // The view models are asked for only once the app and its colors exist; they pick colors as they are made.
    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;

        // A method of the app, not a lambda: MAUI keeps only a weak reference to theme handlers,
        // so a lambda would be garbage-collected and stop being called.
        RequestedThemeChanged += OnRequestedThemeChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new(new AppShell()) { Title = "Check your budget" };
#if WINDOWS
        window.Width = 1100;
        window.Height = 860;
        window.MinimumWidth = 380;
        window.MinimumHeight = 500;
#endif
        return window;
    }

    // The charts and some text colors are picked in code, so they are picked again for the new theme.
    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        services.GetRequiredService<MonthViewModel>().OnThemeChanged();
        services.GetRequiredService<HistoryViewModel>().Refresh();
    }
}
