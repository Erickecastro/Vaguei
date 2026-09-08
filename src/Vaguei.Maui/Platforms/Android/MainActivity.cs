using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity;
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
        OnBackPressedDispatcher.AddCallback(this, new VagueiBackCallback(this));
        ApplySystemBars(AppInfo.RequestedTheme == AppTheme.Dark);
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplySystemBars(AppInfo.RequestedTheme == AppTheme.Dark);
    }

    private bool HandleBackNavigation()
    {
        var currentPage = Microsoft.Maui.Controls.Application.Current?
            .Windows.FirstOrDefault()?.Page;
        if (currentPage is not NavigationPage navigation) return false;

        if (navigation.Navigation.ModalStack.Count > 0)
        {
            _ = navigation.Navigation.PopModalAsync();
            return true;
        }

        return navigation.CurrentPage is MainPage mainPage &&
               mainPage.HandleSystemBack();
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

    public void CloseApplication() => FinishAndRemoveTask();

    private sealed class VagueiBackCallback(MainActivity activity)
        : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            if (activity.HandleBackNavigation()) return;

            Enabled = false;
            activity.OnBackPressedDispatcher.OnBackPressed();
            Enabled = true;
        }
    }
}
