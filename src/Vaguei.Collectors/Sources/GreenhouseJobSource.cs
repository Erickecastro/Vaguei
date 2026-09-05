using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class GreenhouseJobSource : IJobSource
{
    public static readonly IReadOnlyDictionary<string, string> DefaultBoards =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["intersystems"] = "InterSystems",
            ["braze"] = "Braze",
            ["atolls"] = "Atolls",
            ["qualtrics"] = "Qualtrics",
            ["gitlab"] = "GitLab",
            ["ebanx"] = "EBANX",
            ["monks"] = "Monks",
            ["quintoandar"] = "QuintoAndar",
            ["gympass"] = "Wellhub"
        };

    private readonly HttpClient _httpClient;
    private readonly IReadOnlyDictionary<string, string> _boards;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public GreenhouseJobSource(
        HttpClient httpClient,
        IReadOnlyDictionary<string, string>? boards = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _boards = boards ?? DefaultBoards;
    }

    public string Name => "Greenhouse";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var searches = _boards.Select(board =>
            SearchBoardAsync(board.Key, board.Value, cancellationToken));
        var results = await Task.WhenAll(searches);

        if (results.All(result => !result.Succeeded))
        {
            throw new HttpRequestException(
                "Nenhum quadro Greenhouse pôde ser consultado.");
        }

        return results
            .SelectMany(result => result.Jobs)
            .Where(job => JobSourceMapping.MatchesQuery(job, query))
            .ToArray();
    }

    private async Task<BoardResult> SearchBoardAsync(
        string boardToken,
        string company,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://boards-api.greenhouse.io/v1/boards/{Uri.EscapeDataString(boardToken)}/jobs?content=true";
            var response = await _httpClient.GetFromJsonAsync<GreenhouseResponse>(
                url,
                cancellationToken);

            return new BoardResult(
                true,
                response?.Jobs.Select(job => MapJob(job, boardToken, company)).ToArray() ?? []);
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

    private JobPosting MapJob(GreenhouseJob job, string boardName, string company)
    {
        var posting = new JobPosting
        {
            Title = job.Title,
            Company = company,
            Description = JobSourceMapping.PlainText(job.Content),
            Location = JobSourceMapping.MapLocation(job.Location?.Name),
            Url = Uri.TryCreate(job.AbsoluteUrl, UriKind.Absolute, out var uri)
                ? uri
                : null,
            Source = Name,
            SourcePostingId = job.Id == 0 ? null : $"{boardName}:{job.Id}",
            WorkModel = JobSourceMapping.MapWorkModel(job.Location?.Name),
            PublishedAt = job.UpdatedAt,
            Tags = job.Departments
                .Select(department => department.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };

        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private sealed record BoardResult(bool Succeeded, IReadOnlyCollection<JobPosting> Jobs);

    private sealed class GreenhouseResponse
    {
        [JsonPropertyName("jobs")]
        public List<GreenhouseJob> Jobs { get; init; } = [];
    }

    private sealed class GreenhouseJob
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("absolute_url")]
        public string? AbsoluteUrl { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset? UpdatedAt { get; init; }

        [JsonPropertyName("location")]
        public GreenhouseLocation? Location { get; init; }

        [JsonPropertyName("departments")]
        public List<GreenhouseDepartment> Departments { get; init; } = [];
    }

    private sealed class GreenhouseLocation
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class GreenhouseDepartment
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }
}
