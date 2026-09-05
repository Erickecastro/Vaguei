using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Vaguei.Application.Services;
using Vaguei.Collectors.Configuration;
using Vaguei.Desktop.ViewModels;
using Vaguei.Desktop.Views;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;
using Vaguei.Infrastructure;

namespace Vaguei.Desktop;

public partial class App : Avalonia.Application
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            var themeStore = new JsonThemePreferenceStore();
            RequestedThemeVariant = themeStore.Load() == "Dark"
                ? ThemeVariant.Dark
                : ThemeVariant.Light;

            var parserService = new ResumeParserService(
            [
                new OdtResumeParser(),
                new DocxResumeParser(),
                new PdfResumeParser(),
                new TextResumeParser()
            ]);

            var sources = JobSourceFactory.Create(_httpClient);

            desktop.MainWindow = new MainWindow(themeStore)
            {
                DataContext = new MainViewModel(
                    parserService,
                    new ResumeAnalyzer(),
                    new JobSearchOrchestrator(sources),
                    new JsonFavoriteJobStore(),
                    new JsonJobSearchSettingsStore())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
