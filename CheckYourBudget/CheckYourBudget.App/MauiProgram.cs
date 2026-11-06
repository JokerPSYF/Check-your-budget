using CheckYourBudget.App.Services;
using CheckYourBudget.App.ViewModels;
using CheckYourBudget.App.Views;

namespace CheckYourBudget.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // One of each for the whole app: the screens share the same budget book.
        builder.Services.AddSingleton<BudgetSession>();
        builder.Services.AddSingleton<MonthViewModel>();
        builder.Services.AddSingleton<HistoryViewModel>();
        builder.Services.AddSingleton<RuleViewModel>();
        builder.Services.AddSingleton<MonthPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<RulePage>();

        return builder.Build();
    }
}
