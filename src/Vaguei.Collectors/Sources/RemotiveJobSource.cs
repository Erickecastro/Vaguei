using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class RemotiveJobSource : IJobSource
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _cacheGate = new(1, 1);
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();
    private IReadOnlyCollection<JobPosting> _cachedJobs = [];
    private DateTimeOffset _cacheExpiresAt;

    public RemotiveJobSource(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public string Name => "Remotive";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var jobs = await GetCurrentJobsAsync(cancellationToken);
        return jobs.Where(job => JobSourceMapping.MatchesQuery(job, query)).ToArray();
    }

    private async Task<IReadOnlyCollection<JobPosting>> GetCurrentJobsAsync(
        CancellationToken cancellationToken)
    {
        if (_cacheExpiresAt > DateTimeOffset.UtcNow) return _cachedJobs;

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (_cacheExpiresAt > DateTimeOffset.UtcNow) return _cachedJobs;

            var response = await _httpClient.GetFromJsonAsync<RemotiveResponse>(
                "https://remotive.com/api/remote-jobs",
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

    private JobPosting MapJob(RemotiveJob job)
    {
        var posting = new JobPosting
        {
            Title = job.Title,
            Company = job.Company,
            Description = JobSourceMapping.PlainText(job.Description),
            Location = JobSourceMapping.MapLocation(job.Location),
            Url = Uri.TryCreate(job.Url, UriKind.Absolute, out var uri) ? uri : null,
            Source = Name,
            SourcePostingId = job.Id.ToString(CultureInfo.InvariantCulture),
            WorkModel = WorkModel.Remote,
            EmploymentType = JobSourceMapping.MapEmploymentType(job.JobType),
            PublishedAt = job.PublishedAt,
            Tags = new[] { job.Category, job.JobType }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private sealed class RemotiveResponse
    {
        [JsonPropertyName("jobs")] public List<RemotiveJob> Jobs { get; init; } = [];
    }

    private sealed class RemotiveJob
    {
        [JsonPropertyName("id")] public long Id { get; init; }
        [JsonPropertyName("url")] public string? Url { get; init; }
        [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
        [JsonPropertyName("company_name")] public string Company { get; init; } = string.Empty;
        [JsonPropertyName("category")] public string? Category { get; init; }
        [JsonPropertyName("job_type")] public string? JobType { get; init; }
        [JsonPropertyName("publication_date")] public DateTimeOffset? PublishedAt { get; init; }
        [JsonPropertyName("candidate_required_location")] public string? Location { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
    }
}
