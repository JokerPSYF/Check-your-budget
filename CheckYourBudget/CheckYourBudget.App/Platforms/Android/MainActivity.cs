using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;

namespace CheckYourBudget.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        UpdateSystemBarIcons();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        UpdateSystemBarIcons();
    }

    // Dark clock and battery icons on the light theme, light ones on the dark theme.
    private void UpdateSystemBarIcons()
    {
        if (Window is not { DecorView: { } decor } window
            || WindowCompat.GetInsetsController(window, decor) is not { } controller)
        {
            return;
        }

        bool dark = (Resources?.Configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;
        controller.AppearanceLightStatusBars = !dark;
        controller.AppearanceLightNavigationBars = !dark;
    }
}
