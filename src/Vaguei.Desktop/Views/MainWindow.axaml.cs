using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Vaguei.Desktop.ViewModels;

namespace Vaguei.Desktop.Views;

public partial class MainWindow : Window
{
    private const double NarrowLayoutThreshold = 1080;
    private bool? _isCompactLayout;
    private bool _isThemeTransitioning;

    private static readonly FilePickerFileType ResumeFileType = new("Currículos")
    {
        Patterns = ["*.odt", "*.docx", "*.pdf", "*.txt"]
    };

    public MainWindow()
    {
        InitializeComponent();

        ThemeTransitionOverlay.Transitions =
        [
            new DoubleTransition
            {
                Property = OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(320)
            }
        ];

        SizeChanged += OnWindowSizeChanged;
        PropertyChanged += OnWindowPropertyChanged;
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
        if (Avalonia.Application.Current is null || _isThemeTransitioning)
        {
            return;
        }

        _isThemeTransitioning = true;
        var currentTheme = Avalonia.Application.Current.ActualThemeVariant;
        ThemeTransitionOverlay.Background = new SolidColorBrush(
            currentTheme == ThemeVariant.Dark
                ? Color.Parse("#121212")
                : Color.Parse("#F6F7FB"));
        ThemeTransitionOverlay.Transitions = null;
        ThemeTransitionOverlay.Opacity = 1;
        ThemeTransitionOverlay.IsVisible = true;

        var requestedTheme =
            currentTheme == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

        Avalonia.Application.Current.RequestedThemeVariant = requestedTheme;
        UpdateThemeVisuals(requestedTheme);

        await Dispatcher.UIThread.InvokeAsync(
            () => { },
            DispatcherPriority.Render);

        ThemeTransitionOverlay.Transitions =
        [
            new DoubleTransition
            {
                Property = OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(320)
            }
        ];
        ThemeTransitionOverlay.Opacity = 0;
        await Task.Delay(340);
        ThemeTransitionOverlay.IsVisible = false;
        _isThemeTransitioning = false;
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

    private void OnWindowPropertyChanged(
        object? sender,
        AvaloniaPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.Property == WindowStateProperty)
        {
            WindowSurface.CornerRadius = WindowState == WindowState.Maximized
                ? new CornerRadius(0)
                : new CornerRadius(12);
        }
    }

    private void UpdateAdaptiveLayout(
        double width)
    {
        var isCompact = width < NarrowLayoutThreshold;
        SidebarSplitView.DisplayMode = isCompact
            ? SplitViewDisplayMode.Overlay
            : SplitViewDisplayMode.Inline;
        SidebarSplitView.OpenPaneLength = 250;
        CompactSidebarButton.IsVisible = isCompact;
        JobSearchField.Width = isCompact ? 260 : 420;
        PublicationSelector.Width = isCompact ? 160 : 190;

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
        TitleDarkThemeLogo.IsVisible = isDark;
        TitleLightThemeLogo.IsVisible = !isDark;
        ScopeSelector.Foreground = foreground;
        ScopeLocationIcon.Foreground = foreground;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs eventArgs) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs eventArgs) =>
        ToggleMaximizedState();

    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs) =>
        Close();

    private void OnTitleBarPointerPressed(
        object? sender,
        PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (eventArgs.ClickCount == 2)
        {
            ToggleMaximizedState();
            return;
        }

        BeginMoveDrag(eventArgs);
    }

    private void OnResizeGripPointerPressed(
        object? sender,
        PointerPressedEventArgs eventArgs)
    {
        if (WindowState != WindowState.Normal ||
            sender is not Control { Tag: string edgeName } ||
            !Enum.TryParse<WindowEdge>(edgeName, out var edge) ||
            !eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginResizeDrag(edge, eventArgs);
    }

    private void ToggleMaximizedState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
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
