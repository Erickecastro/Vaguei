using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Vaguei.Mobile.Views;

public partial class MainView : UserControl
{
    public MainView() => InitializeComponent();

    private async void OnOpenJobClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button { Tag: string url } &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            TopLevel.GetTopLevel(this)?.Launcher is { } launcher)
        {
            await launcher.LaunchUriAsync(uri);
        }
    }
}
