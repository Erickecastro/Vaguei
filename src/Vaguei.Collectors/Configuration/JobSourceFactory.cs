using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Collectors.Sources;

namespace Vaguei.Collectors.Configuration;

public static class JobSourceFactory
{
    public static IReadOnlyCollection<IJobSource> Create(
        HttpClient httpClient,
        JobSourceCatalog? catalog = null,
        string? joobleApiKey = null,
        TimeSpan? sourceTimeout = null,
        int retryCount = 1,
        int maximumConcurrentSources = 3)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        catalog ??= JobSourceCatalog.Load();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumConcurrentSources);
        var concurrencyGate = new SemaphoreSlim(
            maximumConcurrentSources,
            maximumConcurrentSources);
        var sources = new List<IJobSource>
        {
            new ArbeitnowJobSource(httpClient),
            new AshbyJobSource(httpClient, catalog.Ashby),
            new GreenhouseJobSource(httpClient, catalog.Greenhouse),
            new InHireJobSource(httpClient, catalog.InHire),
            new JobicyJobSource(httpClient),
            new LeverJobSource(httpClient, catalog.Lever),
            new RemotiveJobSource(httpClient),
            new SmartRecruitersJobSource(httpClient, catalog.SmartRecruiters),
            new WorkableJobSource(httpClient, catalog.Workable)
        };

        joobleApiKey ??= Environment.GetEnvironmentVariable("JOOBLE_API_KEY");
        if (!string.IsNullOrWhiteSpace(joobleApiKey))
        {
            sources.Add(new JoobleJobSource(httpClient, joobleApiKey));
        }

        return sources
            .Select(source => (IJobSource)new ResilientJobSource(
                source,
                concurrencyGate,
                timeout: sourceTimeout ?? TimeSpan.FromSeconds(25),
                cacheDuration: TimeSpan.FromMinutes(5),
                retryCount: retryCount,
                maximumCacheEntries: 32))
            .ToArray();
    }
}
