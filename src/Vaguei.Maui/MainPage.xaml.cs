using Vaguei.Desktop.ViewModels;
using Vaguei.Infrastructure;
using System.ComponentModel;

namespace Vaguei.Maui;

public partial class MainPage : ContentPage
{
    private Action<int>? _applyFilterSelection;
    private CancellationTokenSource? _searchPulseCancellation;
    private CancellationTokenSource? _networkLossDebounceCancellation;

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
        _networkLossDebounceCancellation?.Cancel();
        ResetTransientUi();
        base.OnDisappearing();
    }

    public void ResetTransientUi()
    {
        FiltersSheet.IsVisible = false;
        FilterOptionsOverlay.IsVisible = false;
        ExitConfirmationOverlay.IsVisible = false;
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

    private void OnSearchButtonClicked(object? sender, EventArgs eventArgs) =>
        SearchEntry.Unfocus();

    private void OnSearchEntryCompleted(object? sender, EventArgs eventArgs)
    {
        SearchEntry.Unfocus();
        if (ViewModel.RefreshJobsCommand.CanExecute(null))
            ViewModel.RefreshJobsCommand.Execute(null);
    }

    private void OnFiltersCloseClicked(object? sender, EventArgs eventArgs) =>
        FiltersSheet.IsVisible = false;

    private void OnFiltersBackdropClicked(object? sender, EventArgs eventArgs) =>
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
        FilterOptionsListHost.Clear();
        for (var index = 0; index < options.Count; index++)
        {
            var optionButton = new Button
            {
                Text = $"{(index == selectedIndex ? "●" : "○")}   {options[index]}",
                CommandParameter = index,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 2)
            };
            optionButton.Clicked += OnFilterOptionClicked;
            FilterOptionsListHost.Add(optionButton);
        }
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

    private void OnFilterOptionsBackdropClicked(object? sender, EventArgs eventArgs) =>
        FilterOptionsOverlay.IsVisible = false;

    private async void OnOpenJobRequested(object? sender, string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
    }

    private void OnScrollToTopClicked(object? sender, EventArgs eventArgs) =>
        JobsCollection.ScrollToTop();

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
        JobsCollection.RefreshTheme();
        new JsonThemePreferenceStore().Save(
            app.UserAppTheme == AppTheme.Dark ? "Dark" : "Light");
    }

    protected override bool OnBackButtonPressed()
        => HandleSystemBack();

    public bool HandleSystemBack()
    {
        if (ExitConfirmationOverlay.IsVisible)
        {
            ExitConfirmationOverlay.IsVisible = false;
            return true;
        }

        if (FilterOptionsOverlay.IsVisible)
        {
            FilterOptionsOverlay.IsVisible = false;
            return true;
        }

        if (FiltersSheet.IsVisible)
        {
            FiltersSheet.IsVisible = false;
            return true;
        }

        if (ViewModel.ShowResultsContent || ViewModel.ShowSearchProgress)
        {
            if (ViewModel.ReturnToSearchCommand.CanExecute(null))
                ViewModel.ReturnToSearchCommand.Execute(null);
            return true;
        }

        ExitConfirmationOverlay.IsVisible = true;
        return true;
    }

    private void OnExitCancelClicked(object? sender, EventArgs eventArgs) =>
        ExitConfirmationOverlay.IsVisible = false;

    private void OnExitBackdropClicked(object? sender, EventArgs eventArgs) =>
        ExitConfirmationOverlay.IsVisible = false;

    private void OnExitConfirmClicked(object? sender, EventArgs eventArgs)
    {
        ExitConfirmationOverlay.IsVisible = false;
        if (Platform.CurrentActivity is MainActivity activity)
            activity.CloseApplication();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs eventArgs)
    {
        _networkLossDebounceCancellation?.Cancel();
        _networkLossDebounceCancellation?.Dispose();
        _networkLossDebounceCancellation = null;

        if (eventArgs.NetworkAccess == NetworkAccess.Internet) return;

        _networkLossDebounceCancellation = new CancellationTokenSource();
        _ = ConfirmNetworkLossAsync(_networkLossDebounceCancellation.Token);
    }

    private async Task ConfirmNetworkLossAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                MainThread.BeginInvokeOnMainThread(() =>
                    ViewModel.HandleNetworkAvailabilityChanged(false));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
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
                await SearchButton.FadeToAsync(0.82, 560, Easing.SinInOut);
                await SearchButton.FadeToAsync(1, 560, Easing.SinInOut);
            }
        }
        finally
        {
            SearchButton.Opacity = 1;
            SearchButton.Scale = 1;
        }
    }

}
