using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class LeverJobSource : IJobSource
{
    public static readonly IReadOnlyDictionary<string, string> DefaultSites =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["swile"] = "Swile",
            ["lalamove"] = "Lalamove",
            ["tryjeeves"] = "Jeeves",
            ["loadsmart"] = "Loadsmart",
            ["ciandt"] = "CI&T",
            ["dlocal"] = "dLocal"
        };

    private readonly HttpClient _httpClient;
    private readonly IReadOnlyDictionary<string, string> _sites;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public LeverJobSource(
        HttpClient httpClient,
        IReadOnlyDictionary<string, string>? sites = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _sites = sites ?? DefaultSites;
    }

    public string Name => "Lever";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var searches = _sites.Select(site =>
            SearchSiteAsync(site.Key, site.Value, cancellationToken));
        var results = await Task.WhenAll(searches);

        if (results.All(result => !result.Succeeded))
        {
            throw new HttpRequestException(
                "Nenhum quadro Lever pôde ser consultado.");
        }

        return results
            .SelectMany(result => result.Jobs)
            .Where(job => JobSourceMapping.MatchesQuery(job, query))
            .ToArray();
    }

    private async Task<SiteResult> SearchSiteAsync(
        string site,
        string company,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://api.lever.co/v0/postings/{Uri.EscapeDataString(site)}?mode=json";
            var jobs = await _httpClient.GetFromJsonAsync<List<LeverJob>>(
                url,
                cancellationToken);

            return new SiteResult(
                true,
                jobs?.Select(job => MapJob(job, site, company)).ToArray() ?? []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new SiteResult(false, []);
        }
    }

    private JobPosting MapJob(LeverJob job, string siteName, string company)
    {
        var location = job.Categories?.Location;
        var description = string.Join(
            " ",
            new[] { job.DescriptionPlain, job.AdditionalPlain }
                .Concat(job.Lists.Select(item => item.Content))
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        var posting = new JobPosting
        {
            Title = job.Text,
            Company = company,
            Description = JobSourceMapping.PlainText(description),
            Location = JobSourceMapping.MapLocation(location),
            Url = Uri.TryCreate(job.HostedUrl, UriKind.Absolute, out var uri)
                ? uri
                : null,
            Source = Name,
            SourcePostingId = string.IsNullOrWhiteSpace(job.Id)
                ? null
                : $"{siteName}:{job.Id}",
            WorkModel = JobSourceMapping.MapWorkModel(location, job.WorkplaceType),
            EmploymentType = JobSourceMapping.MapEmploymentType(job.Categories?.Commitment),
            PublishedAt = job.CreatedAt is > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(job.CreatedAt.Value)
                : null,
            Tags = new[] { job.Categories?.Team, job.Categories?.Department }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };

        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private sealed record SiteResult(bool Succeeded, IReadOnlyCollection<JobPosting> Jobs);

    private sealed class LeverJob
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;

        [JsonPropertyName("descriptionPlain")]
        public string? DescriptionPlain { get; init; }

        [JsonPropertyName("additionalPlain")]
        public string? AdditionalPlain { get; init; }

        [JsonPropertyName("hostedUrl")]
        public string? HostedUrl { get; init; }

        [JsonPropertyName("createdAt")]
        public long? CreatedAt { get; init; }

        [JsonPropertyName("workplaceType")]
        public string? WorkplaceType { get; init; }

        [JsonPropertyName("categories")]
        public LeverCategories? Categories { get; init; }

        [JsonPropertyName("lists")]
        public List<LeverList> Lists { get; init; } = [];
    }

    private sealed class LeverCategories
    {
        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("commitment")]
        public string? Commitment { get; init; }

        [JsonPropertyName("team")]
        public string? Team { get; init; }

        [JsonPropertyName("department")]
        public string? Department { get; init; }
    }

    private sealed class LeverList
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}
