using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class ArbeitnowJobSource : IJobSource
{
    private readonly HttpClient _httpClient;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer;

    public ArbeitnowJobSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _requirementAnalyzer = new JobSkillRequirementAnalyzer();
    }

    public string Name => "Arbeitnow";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var response =
            await _httpClient.GetFromJsonAsync<ArbeitnowResponse>(
                "https://www.arbeitnow.com/api/job-board-api",
                cancellationToken);

        if (response is null)
        {
            return [];
        }

        var jobs = response.Data
            .Select(MapJob)
            .Where(job => MatchesQuery(job, query))
            .ToList();

        return jobs;
    }

    private static DateTimeOffset? ConvertPublishedAt(
        long? createdAt)
    {
        if (createdAt is null ||
            createdAt <= 0)
        {
            return null;
        }

        try
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(
                    createdAt.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private JobPosting MapJob(ArbeitnowJob job)
    {
        var posting = new JobPosting
        {
            Title = job.Title,
            Company = job.CompanyName,
            Description = job.Description ?? string.Empty,

            Location = new JobLocation
            {
                RawLocation = job.Location
            },

            Url = Uri.TryCreate(
                job.Url,
                UriKind.Absolute,
                out var uri)
                ? uri
                : null,

            Source = "Arbeitnow",
            SourcePostingId = job.Slug,

            WorkModel = job.Remote
                ? WorkModel.Remote
                : WorkModel.Unknown,

            PublishedAt =
                ConvertPublishedAt(
                    job.CreatedAt),

            Tags = job.Tags
                .Where(tag =>
                    !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };

        posting.SkillRequirements =
            _requirementAnalyzer
                .Analyze(posting)
                .ToList();

        return posting;
    }

    private static bool MatchesQuery(
        JobPosting job,
        JobSearchQuery query)
    {
        if (!JobSourceMapping.MatchesQuery(job, query))
        {
            return false;
        }

        if (query.WorkModels.Count > 0 &&
            !query.WorkModels.Contains(job.WorkModel))
        {
            return false;
        }

        if (query.Locations.Count > 0)
        {
            var rawLocation =
                job.Location.RawLocation ?? string.Empty;

            var matchesLocation =
                query.Locations.Any(
                    location =>
                        rawLocation.Contains(
                            location,
                            StringComparison.OrdinalIgnoreCase));

            if (!matchesLocation)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ArbeitnowResponse
    {
        [JsonPropertyName("data")]
        public List<ArbeitnowJob> Data { get; init; } = [];
    }

    private sealed class ArbeitnowJob
    {
        [JsonPropertyName("slug")]
        public string Slug { get; init; } = string.Empty;

        [JsonPropertyName("company_name")]
        public string CompanyName { get; init; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("remote")]
        public bool Remote { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; init; } = [];

        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; init; }
    }
}
