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
                // Catálogos brasileiros são consultados por empresa. Duas
                // palavras-chave e quatro segundos por empresa produzem uma
                // primeira cobertura útil sem deixar uma empresa lenta consumir
                // todo o orçamento da pesquisa móvel.
                smartRecruitersMaximumSearchTerms: 2,
                smartRecruitersCompanyTimeout: TimeSpan.FromSeconds(4),
                // Para a primeira cobertura móvel, título, local, data e áreas
                // são suficientes para filtrar. Evitamos baixar HTML completo
                // de todos os quadros Greenhouse antes de mostrar as vagas.
                greenhouseIncludeContent: false,
                // The very large Greenhouse payload can take tens of seconds to
                // deserialize on Android. Other sources retain their disk cache;
                // Greenhouse uses only the fast in-memory cache on mobile.
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
