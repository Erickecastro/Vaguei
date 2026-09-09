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
        TimeSpan? cacheDuration = null,
        int retryCount = 1,
        int maximumConcurrentSources = 3,
        IPersistentJobCache? persistentCache = null,
        IReadOnlySet<string>? persistentCacheExclusions = null,
        int smartRecruitersMaximumSearchTerms = 6,
        TimeSpan? smartRecruitersCompanyTimeout = null,
        bool greenhouseIncludeContent = true)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        catalog ??= JobSourceCatalog.Load();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumConcurrentSources);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            smartRecruitersMaximumSearchTerms);
        var concurrencyGate = new SemaphoreSlim(
            maximumConcurrentSources,
            maximumConcurrentSources);
        var sources = new List<IJobSource>
        {
            new ArbeitnowJobSource(httpClient),
            new JobicyJobSource(httpClient),
            new RemotiveJobSource(httpClient),
            new AshbyJobSource(httpClient, catalog.Ashby),
            new GreenhouseJobSource(
                httpClient,
                catalog.Greenhouse,
                greenhouseIncludeContent),
            new InHireJobSource(httpClient, catalog.InHire),
            new LeverJobSource(httpClient, catalog.Lever),
            new SmartRecruitersJobSource(
                httpClient,
                catalog.SmartRecruiters,
                smartRecruitersMaximumSearchTerms,
                smartRecruitersCompanyTimeout),
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
                cacheDuration: cacheDuration ?? TimeSpan.FromMinutes(5),
                retryCount: retryCount,
                maximumCacheEntries: 32,
                persistentCache: persistentCacheExclusions?.Contains(source.Name) == true
                    ? null
                    : persistentCache))
            .ToArray();
    }
}
