using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class AshbyJobSource : IJobSource
{
    public static readonly IReadOnlyDictionary<string, string> DefaultBoards =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reonic"] = "Reonic"
        };

    private readonly HttpClient _httpClient;
    private readonly IReadOnlyDictionary<string, string> _boards;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public AshbyJobSource(
        HttpClient httpClient,
        IReadOnlyDictionary<string, string>? boards = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _boards = boards ?? DefaultBoards;
    }

    public string Name => "Ashby";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var searches = _boards.Select(board =>
            SearchBoardAsync(board.Key, board.Value, cancellationToken));
        var results = await Task.WhenAll(searches);

        if (results.All(result => !result.Succeeded))
        {
            throw new HttpRequestException("Nenhum quadro Ashby pôde ser consultado.");
        }

        return results
            .SelectMany(result => result.Jobs)
            .Where(job => JobSourceMapping.MatchesQuery(job, query))
            .ToArray();
    }

    private async Task<BoardResult> SearchBoardAsync(
        string boardName,
        string company,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://api.ashbyhq.com/posting-api/job-board/{Uri.EscapeDataString(boardName)}";
            var response = await _httpClient.GetFromJsonAsync<AshbyResponse>(url, cancellationToken);

            return new BoardResult(
                true,
                response?.Jobs
                    .Where(job => job.IsListed)
                    .Select(job => MapJob(job, company))
                    .ToArray() ?? []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new BoardResult(false, []);
        }
    }

    private JobPosting MapJob(AshbyJob job, string company)
    {
        var posting = new JobPosting
        {
            Title = job.Title,
            Company = company,
            Description = JobSourceMapping.PlainText(job.DescriptionPlain),
            Location = JobSourceMapping.MapLocation(job.Location),
            Url = Uri.TryCreate(job.JobUrl, UriKind.Absolute, out var uri) ? uri : null,
            Source = Name,
            WorkModel = job.IsRemote == true
                ? WorkModel.Remote
                : JobSourceMapping.MapWorkModel(job.Location, job.WorkplaceType),
            EmploymentType = JobSourceMapping.MapEmploymentType(job.EmploymentType),
            PublishedAt = job.PublishedAt,
            Tags = new[] { job.Department, job.Team }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };

        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private sealed record BoardResult(bool Succeeded, IReadOnlyCollection<JobPosting> Jobs);

    private sealed class AshbyResponse
    {
        [JsonPropertyName("jobs")]
        public List<AshbyJob> Jobs { get; init; } = [];
    }

    private sealed class AshbyJob
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("descriptionPlain")]
        public string? DescriptionPlain { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("publishedAt")]
        public DateTimeOffset? PublishedAt { get; init; }

        [JsonPropertyName("isListed")]
        public bool IsListed { get; init; }

        [JsonPropertyName("isRemote")]
        public bool? IsRemote { get; init; }

        [JsonPropertyName("workplaceType")]
        public string? WorkplaceType { get; init; }

        [JsonPropertyName("employmentType")]
        public string? EmploymentType { get; init; }

        [JsonPropertyName("department")]
        public string? Department { get; init; }

        [JsonPropertyName("team")]
        public string? Team { get; init; }

        [JsonPropertyName("jobUrl")]
        public string? JobUrl { get; init; }
    }
}
