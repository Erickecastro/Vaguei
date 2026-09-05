using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Vaguei.Android;

[Activity(
    Label = "Vaguei",
    Theme = "@style/VagueiTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation |
                           ConfigChanges.ScreenSize |
                           ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
