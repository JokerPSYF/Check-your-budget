namespace CheckYourBudget.App.WinUI;

/// <summary>The Windows entry point; everything else is shared with Android.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
