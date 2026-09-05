using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Vaguei.Desktop.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        AboutTitleBar.AddHandler(
            PointerPressedEvent,
            OnTitleBarPointerPressed,
            RoutingStrategies.Tunnel);
        Opened += (_, _) => UpdateThemeLogo();
    }

    private void UpdateThemeLogo()
    {
        var isDark = Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        DarkThemeLogo.IsVisible = isDark;
        LightThemeLogo.IsVisible = !isDark;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.Source is Button ||
            !eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginMoveDrag(eventArgs);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs) => Close();
}
