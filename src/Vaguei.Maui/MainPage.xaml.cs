using Vaguei.Desktop.ViewModels;
using Vaguei.Infrastructure;
using System.ComponentModel;

namespace Vaguei.Maui;

public partial class MainPage : ContentPage
{
    private Action<int>? _applyFilterSelection;
    private CancellationTokenSource? _searchPulseCancellation;

    private static readonly FilePickerFileType ResumeFiles = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.Android] =
            [
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.oasis.opendocument.text",
                "text/plain"
            ]
        });

    private MainViewModel ViewModel => (MainViewModel)BindingContext;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ResetTransientUi();
        var app = Microsoft.Maui.Controls.Application.Current;
        if (app is not null && Platform.CurrentActivity is MainActivity activity)
            activity.ApplySystemBars(app.RequestedTheme == AppTheme.Dark);
        UpdateSearchPulse();
    }

    protected override void OnDisappearing()
    {
        ResetTransientUi();
        base.OnDisappearing();
    }

    public void ResetTransientUi()
    {
        FiltersSheet.IsVisible = false;
        FilterOptionsOverlay.IsVisible = false;
    }

    private async void OnChooseResumeClicked(object? sender, EventArgs eventArgs)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Escolha seu currículo",
            FileTypes = ResumeFiles
        });
        if (file is null) return;

        var extension = Path.GetExtension(file.FileName);
        var temporaryPath = Path.Combine(
            FileSystem.CacheDirectory,
            $"resume-{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var source = await file.OpenReadAsync())
            await using (var destination = File.Create(temporaryPath))
                await source.CopyToAsync(destination);

            await ViewModel.ProcessResumeAsync(temporaryPath);
            if (ViewModel.HasProfile) ViewModel.SelectedFileName = file.FileName;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private void OnFiltersClicked(object? sender, EventArgs eventArgs) =>
        FiltersSheet.IsVisible = true;

    private void OnFiltersCloseClicked(object? sender, EventArgs eventArgs) =>
        FiltersSheet.IsVisible = false;

    private void OnSearchScopeFilterClicked(object? sender, EventArgs eventArgs) =>
        ShowFilterOptions("Região", ViewModel.SearchScopes, ViewModel.SearchScopeIndex,
            value => ViewModel.SearchScopeIndex = value);

    private void OnPublicationFilterClicked(object? sender, EventArgs eventArgs) =>
        ShowFilterOptions("Período", ViewModel.PublicationWindows, ViewModel.PublicationWindowIndex,
            value => ViewModel.PublicationWindowIndex = value);

    private void OnWorkModelFilterClicked(object? sender, EventArgs eventArgs) =>
        ShowFilterOptions("Modelo de trabalho", ViewModel.WorkModelOptions, ViewModel.WorkModelIndex,
            value => ViewModel.WorkModelIndex = value);

    private void OnEmploymentFilterClicked(object? sender, EventArgs eventArgs) =>
        ShowFilterOptions("Tipo de contrato", ViewModel.EmploymentTypeOptions, ViewModel.EmploymentTypeIndex,
            value => ViewModel.EmploymentTypeIndex = value);

    private void OnSeniorityFilterClicked(object? sender, EventArgs eventArgs) =>
        ShowFilterOptions("Senioridade", ViewModel.SeniorityOptions, ViewModel.SeniorityIndex,
            value => ViewModel.SeniorityIndex = value);

    private void ShowFilterOptions(
        string title,
        IReadOnlyList<string> options,
        int selectedIndex,
        Action<int> applySelection)
    {
        FilterOptionsTitle.Text = title;
        FilterOptionsList.ItemsSource = options
            .Select((label, index) => new FilterOption(
                index,
                $"{(index == selectedIndex ? "●" : "○")}   {label}"))
            .ToArray();
        _applyFilterSelection = applySelection;
        FilterOptionsOverlay.IsVisible = true;
    }

    private void OnFilterOptionClicked(object? sender, EventArgs eventArgs)
    {
        if (sender is Button { CommandParameter: int index })
            _applyFilterSelection?.Invoke(index);
        FilterOptionsOverlay.IsVisible = false;
    }

    private void OnFilterOptionsCloseClicked(object? sender, EventArgs eventArgs) =>
        FilterOptionsOverlay.IsVisible = false;

    private async void OnOpenJobClicked(object? sender, EventArgs eventArgs)
    {
        if (sender is Button { CommandParameter: string url } &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Launcher.Default.OpenAsync(uri);
    }

    private void OnScrollToTopClicked(object? sender, EventArgs eventArgs) =>
        JobsCollection.ScrollTo(0, position: ScrollToPosition.Start, animate: false);

    private async void OnAboutClicked(object? sender, EventArgs eventArgs) =>
        await Navigation.PushModalAsync(new NavigationPage(new AboutPage()));

    private void OnThemeClicked(object? sender, EventArgs eventArgs)
    {
        var app = Microsoft.Maui.Controls.Application.Current;
        if (app is null) return;
        app.UserAppTheme = app.RequestedTheme == AppTheme.Dark
            ? AppTheme.Light
            : AppTheme.Dark;
        if (Platform.CurrentActivity is MainActivity activity)
            activity.ApplySystemBars(app.UserAppTheme == AppTheme.Dark);
        new JsonThemePreferenceStore().Save(
            app.UserAppTheme == AppTheme.Dark ? "Dark" : "Light");
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs eventArgs)
    {
        if (eventArgs.NetworkAccess != NetworkAccess.Internet)
            MainThread.BeginInvokeOnMainThread(() =>
                ViewModel.HandleNetworkAvailabilityChanged(false));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(MainViewModel.IsSearchAttentionActive))
            MainThread.BeginInvokeOnMainThread(UpdateSearchPulse);
    }

    private void UpdateSearchPulse()
    {
        _searchPulseCancellation?.Cancel();
        _searchPulseCancellation?.Dispose();
        _searchPulseCancellation = null;
        SearchButton.AbortAnimation("SearchAttentionPulse");
        SearchButton.Opacity = 1;
        SearchButton.Scale = 1;

        if (!ViewModel.IsSearchAttentionActive) return;

        _searchPulseCancellation = new CancellationTokenSource();
        _ = PulseSearchButtonAsync(_searchPulseCancellation.Token);
    }

    private async Task PulseSearchButtonAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && ViewModel.IsSearchAttentionActive)
            {
                await Task.WhenAll(
                    SearchButton.FadeToAsync(0.76, 520, Easing.SinInOut),
                    SearchButton.ScaleToAsync(1.035, 520, Easing.SinInOut));
                await Task.WhenAll(
                    SearchButton.FadeToAsync(1, 520, Easing.SinInOut),
                    SearchButton.ScaleToAsync(1, 520, Easing.SinInOut));
            }
        }
        finally
        {
            SearchButton.Opacity = 1;
            SearchButton.Scale = 1;
        }
    }

    private sealed record FilterOption(int Index, string DisplayText);
}
