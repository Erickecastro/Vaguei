using Microsoft.Extensions.Logging;
using Vaguei.Application.Services;
using Vaguei.Collectors.Configuration;
using Vaguei.Desktop.ViewModels;
using Vaguei.Infrastructure;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;

namespace Vaguei.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.ConfigureMauiHandlers(handlers =>
            handlers.AddHandler<AndroidJobListView, AndroidJobListViewHandler>());

        builder.Services.AddSingleton(_ => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        });
        builder.Services.AddSingleton(provider =>
        {
            using var catalogStream = typeof(MauiProgram).Assembly
                .GetManifestResourceStream("Vaguei.Maui.job-sources.json");
            var catalog = catalogStream is null
                ? JobSourceCatalog.CreateDefault()
                : JobSourceCatalog.Load(catalogStream);
            var sources = JobSourceFactory.Create(
                provider.GetRequiredService<HttpClient>(),
                catalog,
                sourceTimeout: TimeSpan.FromSeconds(12),
                cacheDuration: TimeSpan.FromMinutes(30),
                retryCount: 0,
                maximumConcurrentSources: 9,
                // The very large Greenhouse payload can take tens of seconds to
                // deserialize on Android and cannot be interrupted through the
                // synchronous cache contract. Other sources retain their disk
                // cache; Greenhouse uses only the fast in-memory cache on mobile.
                persistentCache: new JsonPersistentJobCache(),
                persistentCacheExclusions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Greenhouse"
                });

            return new MainViewModel(
                new ResumeParserService(
                [
                    new OdtResumeParser(),
                    new DocxResumeParser(),
                    new PdfResumeParser(),
                    new TextResumeParser()
                ]),
                new ResumeAnalyzer(),
                new JobSearchOrchestrator(sources),
                new JsonFavoriteJobStore(),
                new JsonJobSearchSettingsStore(),
                searchTimeout: TimeSpan.FromSeconds(16),
                networkAvailable: () => Connectivity.Current.NetworkAccess ==
                    NetworkAccess.Internet,
                enableProgressiveSearch: true);
        });
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }

}
