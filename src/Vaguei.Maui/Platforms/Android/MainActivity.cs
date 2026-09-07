using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace Vaguei.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // The search page is deliberately restored from local preferences, not
        // from Android's fragment/dialog stack. This prevents old filter sheets
        // and modal pages from reopening after the process is recreated.
        base.OnCreate(null);
        ApplySystemBars(AppInfo.RequestedTheme == AppTheme.Dark);
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplySystemBars(AppInfo.RequestedTheme == AppTheme.Dark);
    }

    public void ApplySystemBars(bool dark)
    {
        if (Window is null) return;

#pragma warning disable CA1422 // Required fallback for Android 23-34; Android 35 uses edge-to-edge.
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor(dark ? "#121212" : "#F6F7FB"));
        Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor(dark ? "#121212" : "#F6F7FB"));
#pragma warning restore CA1422
        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is null) return;
        controller.AppearanceLightStatusBars = !dark;
        controller.AppearanceLightNavigationBars = !dark;
    }
}
