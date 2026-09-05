using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Collectors.Sources;
using Vaguei.Desktop.ViewModels;
using Vaguei.Desktop.Views;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;

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
            var parserService = new ResumeParserService(
            [
                new OdtResumeParser(),
                new DocxResumeParser(),
                new PdfResumeParser(),
                new TextResumeParser()
            ]);

            var sources = new IJobSource[]
            {
                new ArbeitnowJobSource(_httpClient),
                new AshbyJobSource(_httpClient),
                new GreenhouseJobSource(_httpClient),
                new InHireJobSource(_httpClient),
                new LeverJobSource(_httpClient),
                new SmartRecruitersJobSource(_httpClient)
            };

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    parserService,
                    new ResumeAnalyzer(),
                    new JobSearchOrchestrator(sources))
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
