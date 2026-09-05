using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class JobicyJobSource : IJobSource
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _cacheGate = new(1, 1);
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();
    private IReadOnlyCollection<JobPosting> _cachedJobs = [];
    private DateTimeOffset _cacheExpiresAt;

    public JobicyJobSource(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public string Name => "Jobicy";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var jobs = await GetCurrentJobsAsync(cancellationToken);
        return jobs
            .Where(job => JobSourceMapping.MatchesQuery(job, query))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<JobPosting>> GetCurrentJobsAsync(
        CancellationToken cancellationToken)
    {
        if (_cacheExpiresAt > DateTimeOffset.UtcNow) return _cachedJobs;

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (_cacheExpiresAt > DateTimeOffset.UtcNow) return _cachedJobs;

            var response = await _httpClient.GetFromJsonAsync<JobicyResponse>(
                "https://jobicy.com/api/v2/remote-jobs?count=200",
                cancellationToken);
            _cachedJobs = response?.Jobs.Select(MapJob).ToArray() ?? [];
            _cacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
            return _cachedJobs;
        }
        finally
        {
            _cacheGate.Release();
        }
    }

    private JobPosting MapJob(JobicyJob job)
    {
        var posting = new JobPosting
        {
            Title = job.Title,
            Company = job.Company,
            Description = JobSourceMapping.PlainText(job.Description ?? job.Excerpt),
            Location = JobSourceMapping.MapLocation(job.Geo),
            Url = Uri.TryCreate(job.Url, UriKind.Absolute, out var uri) ? uri : null,
            Source = Name,
            SourcePostingId = job.Id == 0 ? job.Slug : job.Id.ToString(CultureInfo.InvariantCulture),
            WorkModel = WorkModel.Remote,
            EmploymentType = JobSourceMapping.MapEmploymentType(string.Join(' ', job.Types)),
            PublishedAt = DateTimeOffset.TryParse(
                job.PublishedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var publishedAt) ? publishedAt : null,
            Tags = job.Industries
                .Concat(job.Types)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private sealed class JobicyResponse
    {
        [JsonPropertyName("jobs")]
        public List<JobicyJob> Jobs { get; init; } = [];
    }

    private sealed class JobicyJob
    {
        [JsonPropertyName("id")] public long Id { get; init; }
        [JsonPropertyName("jobSlug")] public string? Slug { get; init; }
        [JsonPropertyName("url")] public string? Url { get; init; }
        [JsonPropertyName("jobTitle")] public string Title { get; init; } = string.Empty;
        [JsonPropertyName("companyName")] public string Company { get; init; } = string.Empty;
        [JsonPropertyName("jobGeo")] public string? Geo { get; init; }
        [JsonPropertyName("jobExcerpt")] public string? Excerpt { get; init; }
        [JsonPropertyName("jobDescription")] public string? Description { get; init; }
        [JsonPropertyName("pubDate")] public string? PublishedAt { get; init; }
        [JsonPropertyName("jobIndustry")] public List<string> Industries { get; init; } = [];
        [JsonPropertyName("jobType")] public List<string> Types { get; init; } = [];
    }
}
