using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Vaguei.Application.Services;
using Vaguei.Collectors.Configuration;
using Vaguei.Desktop.ViewModels;
using Vaguei.Infrastructure;
using Vaguei.Mobile.Views;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;

namespace Vaguei.Mobile;

public partial class App : Avalonia.Application
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(22)
    };

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var themeStore = new JsonThemePreferenceStore();
        RequestedThemeVariant = themeStore.Load() == "Dark"
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light;

        if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = CreateMainView;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = CreateMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainView CreateMainView() => new()
    {
        DataContext = new MainViewModel(
            new ResumeParserService(
            [
                new OdtResumeParser(),
                new DocxResumeParser(),
                new PdfResumeParser(),
                new TextResumeParser()
            ]),
            new ResumeAnalyzer(),
            new JobSearchOrchestrator(JobSourceFactory.Create(
                _httpClient,
                sourceTimeout: TimeSpan.FromSeconds(20),
                retryCount: 0,
                maximumConcurrentSources: 5)),
            new JsonFavoriteJobStore(),
            new JsonJobSearchSettingsStore(),
            searchTimeout: TimeSpan.FromSeconds(30),
            networkAvailable: System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable,
            showDetailedSourceWarnings: false)
    };
}
