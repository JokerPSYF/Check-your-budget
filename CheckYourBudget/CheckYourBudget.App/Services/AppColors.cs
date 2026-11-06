using CheckYourBudget.Core;

namespace CheckYourBudget.App.Services;

/// <summary>The colors from Colors.xaml for the current light or dark theme, for code that draws or picks colors.</summary>
public static class AppColors
{
    public static Color Get(string name)
    {
        Application? app = Application.Current;
        string key = name + (app?.RequestedTheme == AppTheme.Dark ? "Dark" : "Light");
        return app is not null && app.Resources.TryGetValue(key, out object value) && value is Color color
            ? color
            : Colors.Gray;
    }

    public static Color Of(Category category) => Get(category.ToString());

    public static Color Text => Get("Text");
    public static Color Muted => Get("Muted");
    public static Color LeftOver => Get("LeftOver");
    public static Color Ok => Get("Ok");
    public static Color Danger => Get("Danger");
    public static Color Warning => Get("Warning");
}
