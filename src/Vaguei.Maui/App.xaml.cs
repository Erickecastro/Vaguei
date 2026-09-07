using Vaguei.Infrastructure;

namespace Vaguei.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
        UserAppTheme = new JsonThemePreferenceStore().Load() == "Dark"
            ? AppTheme.Dark
            : AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(_mainPage));

    protected override void OnSleep()
    {
        _mainPage.ResetTransientUi();
        _ = CloseTransientModalsAsync();
        base.OnSleep();
    }

    private async Task CloseTransientModalsAsync()
    {
        while (_mainPage.Navigation.ModalStack.Count > 0)
            await _mainPage.Navigation.PopModalAsync(false);
    }
}
