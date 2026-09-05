using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Net.NetworkInformation;
using Vaguei.Desktop.ViewModels;
using Vaguei.Infrastructure;

namespace Vaguei.Mobile.Views;

public partial class MainView : UserControl
{
    private bool _introStarted;
    private static readonly FilePickerFileType ResumeFiles = new("Currículos")
    {
        Patterns = ["*.pdf", "*.docx", "*.odt", "*.txt"],
        MimeTypes =
        [
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.oasis.opendocument.text",
            "text/plain"
        ]
    };

    public MainView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private async void OnAttachedToVisualTree(
        object? sender,
        VisualTreeAttachmentEventArgs eventArgs)
    {
        UpdateThemeVisuals();
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        if (!NetworkInterface.GetIsNetworkAvailable() && DataContext is MainViewModel offlineViewModel)
        {
            offlineViewModel.HandleNetworkAvailabilityChanged(false);
        }
        if (_introStarted) return;

        _introStarted = true;
        await Task.Delay(TimeSpan.FromSeconds(3.3));
        IntroPanel.Opacity = 0;
        await Task.Delay(TimeSpan.FromMilliseconds(700));
        IntroPanel.IsVisible = false;
    }

    private void OnDetachedFromVisualTree(
        object? sender,
        VisualTreeAttachmentEventArgs eventArgs) =>
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;

    private void OnNetworkAvailabilityChanged(
        object? sender,
        NetworkAvailabilityEventArgs eventArgs)
    {
        if (eventArgs.IsAvailable) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.HandleNetworkAvailabilityChanged(false);
            }
        });
    }

    private async void OnChooseResumeClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storageProvider ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var file = (await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Escolha seu currículo",
            AllowMultiple = false,
            FileTypeFilter = [ResumeFiles]
        })).FirstOrDefault();

        if (file is null) return;

        var extension = Path.GetExtension(file.Name);
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "Vaguei", "resume-import");
        Directory.CreateDirectory(temporaryDirectory);
        var temporaryPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var source = await file.OpenReadAsync())
            await using (var destination = File.Create(temporaryPath))
            {
                await source.CopyToAsync(destination);
            }

            await viewModel.ProcessResumeAsync(temporaryPath);
            if (viewModel.HasProfile) viewModel.SelectedFileName = file.Name;
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private void OnThemeClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (Avalonia.Application.Current is not { } application) return;

        var theme = application.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
        application.RequestedThemeVariant = theme;
        try
        {
            new JsonThemePreferenceStore().Save(theme == ThemeVariant.Dark ? "Dark" : "Light");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        UpdateThemeVisuals();
    }

    private void UpdateThemeVisuals()
    {
        var isDark = Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        DarkLogo.IsVisible = isDark;
        LightLogo.IsVisible = !isDark;
        IntroDarkLogo.IsVisible = isDark;
        IntroLightLogo.IsVisible = !isDark;
    }

    private void OnFiltersClick(object? sender, RoutedEventArgs eventArgs) =>
        FiltersPanel.IsVisible = !FiltersPanel.IsVisible;

    private void OnFiltersCloseClick(object? sender, RoutedEventArgs eventArgs) =>
        FiltersPanel.IsVisible = false;

    private void OnAboutClick(object? sender, RoutedEventArgs eventArgs) => AboutBackdrop.IsVisible = true;

    private void OnAboutCloseClick(object? sender, RoutedEventArgs eventArgs) => AboutBackdrop.IsVisible = false;

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
