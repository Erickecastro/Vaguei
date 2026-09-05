using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Vaguei.Desktop.Views;

public partial class ExitConfirmationWindow : Window
{
    public ExitConfirmationWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs eventArgs) => Close(false);

    private void OnConfirmClick(object? sender, RoutedEventArgs eventArgs) => Close(true);
}
