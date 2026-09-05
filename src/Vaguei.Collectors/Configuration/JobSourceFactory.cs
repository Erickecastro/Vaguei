using Vaguei.Application.Interfaces;
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

        return
        [
            new ArbeitnowJobSource(httpClient),
            new AshbyJobSource(httpClient, catalog.Ashby),
            new GreenhouseJobSource(httpClient, catalog.Greenhouse),
            new InHireJobSource(httpClient, catalog.InHire),
            new LeverJobSource(httpClient, catalog.Lever),
            new SmartRecruitersJobSource(httpClient, catalog.SmartRecruiters),
            new WorkableJobSource(httpClient, catalog.Workable)
        ];
    }
}
