using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Platform.Storage;
using Vaguei.Desktop.ViewModels;

namespace Vaguei.Desktop.Views;

public partial class MainWindow : Window
{
    private const double NarrowLayoutThreshold = 1080;
    private bool? _isCompactLayout;

    private static readonly FilePickerFileType ResumeFileType = new("Currículos")
    {
        Patterns = ["*.odt", "*.docx", "*.pdf", "*.txt"]
    };

    public MainWindow()
    {
        InitializeComponent();

        Transitions =
        [
            new DoubleTransition
            {
                Property = OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(180)
            }
        ];

        SizeChanged += OnWindowSizeChanged;
        Opened += OnWindowOpened;
    }

    private async void OnChooseFileClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Escolha seu currículo",
                AllowMultiple = false,
                FileTypeFilter = [ResumeFileType]
            });

        var filePath = files
            .FirstOrDefault()?
            .TryGetLocalPath();

        await ProcessFileAsync(filePath);
    }

    private async void OnThemeToggleClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        if (Avalonia.Application.Current is null)
        {
            return;
        }

        Opacity = 0.88;
        await Task.Delay(90);

        var requestedTheme =
            Avalonia.Application.Current.ActualThemeVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

        Avalonia.Application.Current.RequestedThemeVariant = requestedTheme;
        UpdateThemeVisuals(requestedTheme);

        Opacity = 1;
    }

    private void OnWindowOpened(
        object? sender,
        EventArgs eventArgs)
    {
        UpdateAdaptiveLayout(Bounds.Width);
        UpdateThemeVisuals(
            Avalonia.Application.Current?.ActualThemeVariant ??
            ThemeVariant.Light);
    }

    private void OnWindowSizeChanged(
        object? sender,
        SizeChangedEventArgs eventArgs)
    {
        UpdateAdaptiveLayout(eventArgs.NewSize.Width);
    }

    private void UpdateAdaptiveLayout(
        double width)
    {
        var isCompact = width < NarrowLayoutThreshold;
        SidebarSplitView.DisplayMode = isCompact
            ? SplitViewDisplayMode.Overlay
            : SplitViewDisplayMode.Inline;
        SidebarSplitView.OpenPaneLength = isCompact ? 350 : 420;
        CompactSidebarButton.IsVisible = isCompact;

        if (_isCompactLayout != isCompact)
        {
            SidebarSplitView.IsPaneOpen = !isCompact;
            _isCompactLayout = isCompact;
        }

        SidebarPane.Opacity = 1;
    }

    private void OnCompactSidebarClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        SidebarSplitView.IsPaneOpen = true;
    }

    private void UpdateThemeVisuals(
        ThemeVariant themeVariant)
    {
        var isDark = themeVariant == ThemeVariant.Dark;
        var foreground = isDark
            ? Brushes.White
            : Brushes.Black;

        DarkThemeLogo.IsVisible = isDark;
        LightThemeLogo.IsVisible = !isDark;
        ScopeSelector.Foreground = foreground;
        ScopeLocationIcon.Foreground = foreground;
    }

    private void OnOpenJobClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        if (sender is not Button { Tag: string url } ||
            !Uri.TryCreate(
                url,
                UriKind.Absolute,
                out var uri))
        {
            return;
        }

        Process.Start(
            new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
    }

    private void OnDragOver(
        object? sender,
        DragEventArgs eventArgs)
    {
        eventArgs.DragEffects = eventArgs.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(
        object? sender,
        DragEventArgs eventArgs)
    {
        var filePath = eventArgs.DataTransfer
            .TryGetFiles()?
            .FirstOrDefault()?
            .TryGetLocalPath();

        await ProcessFileAsync(filePath);
    }

    private async Task ProcessFileAsync(
        string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        await viewModel.ProcessResumeAsync(filePath);
    }
}
