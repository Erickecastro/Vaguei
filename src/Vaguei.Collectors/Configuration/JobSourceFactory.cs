using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Collectors.Sources;

namespace Vaguei.Collectors.Configuration;

public static class JobSourceFactory
{
    public static IReadOnlyCollection<IJobSource> Create(
        HttpClient httpClient,
        JobSourceCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        catalog ??= JobSourceCatalog.Load();

        var concurrencyGate = new SemaphoreSlim(3, 3);
        var sources = new IJobSource[]
        {
            new ArbeitnowJobSource(httpClient),
            new AshbyJobSource(httpClient, catalog.Ashby),
            new GreenhouseJobSource(httpClient, catalog.Greenhouse),
            new InHireJobSource(httpClient, catalog.InHire),
            new LeverJobSource(httpClient, catalog.Lever),
            new SmartRecruitersJobSource(httpClient, catalog.SmartRecruiters),
            new WorkableJobSource(httpClient, catalog.Workable)
        };

        return sources
            .Select(source => (IJobSource)new ResilientJobSource(
                source,
                concurrencyGate,
                timeout: TimeSpan.FromSeconds(25),
                cacheDuration: TimeSpan.FromMinutes(5),
                retryCount: 1,
                maximumCacheEntries: 32))
            .ToArray();
    }
}
