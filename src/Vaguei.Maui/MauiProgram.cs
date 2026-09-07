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

#if ANDROID
        Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping(
            "VagueiSmoothScrolling",
            (handler, _) =>
            {
                handler.PlatformView.SetItemViewCacheSize(12);
                handler.PlatformView.SetItemAnimator(null);
                handler.PlatformView.NestedScrollingEnabled = true;
                handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Always;
            });
#endif

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
                persistentCache: new JsonPersistentJobCache());

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
                    NetworkAccess.Internet);
        });
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
