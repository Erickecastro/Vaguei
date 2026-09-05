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
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(eventArgs);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs) => Close();
}
