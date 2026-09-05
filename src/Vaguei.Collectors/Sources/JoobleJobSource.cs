using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class JoobleJobSource : IJobSource
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public JoobleJobSource(HttpClient httpClient, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _httpClient = httpClient;
        _apiKey = apiKey.Trim();
    }

    public string Name => "Jooble";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var response = await _httpClient.PostAsJsonAsync(
            $"https://jooble.org/api/{Uri.EscapeDataString(_apiKey)}",
            new JoobleRequest
            {
                Keywords = string.Join(' ', query.Keywords),
                Location = string.Join(", ", query.Locations),
                Page = 1,
                ResultsPerPage = 100
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JoobleResponse>(
            cancellationToken: cancellationToken);

        return (payload?.Jobs ?? [])
            .Select(MapJob)
            .Where(job => JobSourceMapping.MatchesQuery(job, query))
            .ToArray();
    }

    private JobPosting MapJob(JoobleJob job)
    {
        var description = JobSourceMapping.PlainText(job.Snippet);
        var posting = new JobPosting
        {
            Title = JobSourceMapping.PlainText(job.Title),
            Company = JobSourceMapping.PlainText(job.Company),
            Description = description,
            Location = JobSourceMapping.MapLocation(job.Location),
            Url = Uri.TryCreate(job.Link, UriKind.Absolute, out var uri) ? uri : null,
            Source = Name,
            SourcePostingId = ReadIdentifier(job.Id),
            WorkModel = JobSourceMapping.MapWorkModel(job.Location, description),
            EmploymentType = JobSourceMapping.MapEmploymentType(job.Type),
            PublishedAt = DateTimeOffset.TryParse(
                job.Updated,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var publishedAt) ? publishedAt : null,
            Tags = string.IsNullOrWhiteSpace(job.Type)
                ? []
                : new HashSet<string>([job.Type], StringComparer.OrdinalIgnoreCase)
        };
        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private static string? ReadIdentifier(JsonElement identifier) =>
        identifier.ValueKind switch
        {
            JsonValueKind.String => identifier.GetString(),
            JsonValueKind.Number => identifier.GetRawText(),
            _ => null
        };

    private sealed class JoobleRequest
    {
        [JsonPropertyName("keywords")] public string Keywords { get; init; } = string.Empty;
        [JsonPropertyName("location")] public string Location { get; init; } = string.Empty;
        [JsonPropertyName("page")] public int Page { get; init; }
        [JsonPropertyName("ResultOnPage")] public int ResultsPerPage { get; init; }
    }

    private sealed class JoobleResponse
    {
        [JsonPropertyName("jobs")] public List<JoobleJob> Jobs { get; init; } = [];
    }

    private sealed class JoobleJob
    {
        [JsonPropertyName("id")] public JsonElement Id { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("location")] public string? Location { get; init; }
        [JsonPropertyName("snippet")] public string? Snippet { get; init; }
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("link")] public string? Link { get; init; }
        [JsonPropertyName("company")] public string? Company { get; init; }
        [JsonPropertyName("updated")] public string? Updated { get; init; }
    }
}
