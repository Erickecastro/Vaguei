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
            // O limite HTTP precisa ser maior que uma consulta de página
            // pública mais lenta. Antes, 15 segundos encerravam a requisição
            // apesar do orçamento maior configurado para a fonte.
            Timeout = TimeSpan.FromSeconds(45)
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
                // O orçamento móvel precisa acomodar fontes públicas que
                // respondem em ritmos diferentes. Todas continuam em paralelo;
                // ampliar o limite evita perder uma fonte válida por alguns
                // segundos, sem bloquear a interface.
                sourceTimeout: TimeSpan.FromSeconds(40),
                cacheDuration: TimeSpan.FromMinutes(30),
                // Uma repetição protege contra oscilações transitórias de
                // rede. Cinco fontes em paralelo preservam a fluidez sem
                // saturar a pilha de rede de aparelhos intermediários.
                retryCount: 1,
                maximumConcurrentSources: 5,
                // Catálogos brasileiros são consultados por empresa. Oito
                // variações cobrem sinônimos de cargo em português e inglês,
                // incluindo estágio/internship, sem serializar a pesquisa.
                smartRecruitersMaximumSearchTerms: 8,
                smartRecruitersCompanyTimeout: TimeSpan.FromSeconds(10),
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
                },
                // Evita reutilizar entradas pequenas obtidas pela política
                // móvel anterior. Não apaga dados locais; apenas separa o
                // cache de cobertura atual das consultas antigas.
                cacheKeyNamespace: "android-coverage-v3");

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
                searchTimeout: TimeSpan.FromSeconds(60),
                networkAvailable: () => Connectivity.Current.NetworkAccess ==
                    NetworkAccess.Internet);
        });
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }

}
