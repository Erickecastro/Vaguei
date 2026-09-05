using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class WorkableJobSource : IJobSource
{
    public static readonly IReadOnlyDictionary<string, string> DefaultAccounts =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["monkey-1"] = "Monkey",
            ["popmenu"] = "Popmenu",
            ["devpro"] = "Dev.Pro",
            ["pairedrecruiting"] = "Paired",
            ["valsoft-corp"] = "Valsoft Corporation",
            ["whoosh-1"] = "Whoosh"
        };

    private readonly HttpClient _httpClient;
    private readonly IReadOnlyDictionary<string, string> _accounts;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public WorkableJobSource(
        HttpClient httpClient,
        IReadOnlyDictionary<string, string>? accounts = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _accounts = accounts ?? DefaultAccounts;
    }

    public string Name => "Workable";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_accounts.Select(account =>
            SearchAccountAsync(account.Key, account.Value, query, cancellationToken)));

        if (results.All(result => !result.Succeeded))
        {
            throw new HttpRequestException(
                "Nenhuma página pública Workable pôde ser consultada.");
        }

        return results.SelectMany(result => result.Jobs).ToArray();
    }

    private async Task<AccountResult> SearchAccountAsync(
        string account,
        string company,
        JobSearchQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://www.workable.com/api/accounts/{Uri.EscapeDataString(account)}?details=true";
            var response = await _httpClient.GetFromJsonAsync<WorkableResponse>(
                url,
                cancellationToken);
            var jobs = response?.Jobs
                .Select(job => MapJob(job, company))
                .Where(job => JobSourceMapping.MatchesQuery(job, query))
                .ToArray() ?? [];

            return new AccountResult(true, jobs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new AccountResult(false, []);
        }
    }

    private JobPosting MapJob(WorkableJob job, string company)
    {
        var location = string.Join(
            ", ",
            new[] { job.City, job.State, job.Country }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        var posting = new JobPosting
        {
            Title = job.Title,
            Company = company,
            Description = JobSourceMapping.PlainText(job.Description),
            Location = JobSourceMapping.MapLocation(location),
            Url = Uri.TryCreate(job.Url, UriKind.Absolute, out var uri) ? uri : null,
            Source = Name,
            WorkModel = job.Telecommuting
                ? WorkModel.Remote
                : JobSourceMapping.MapWorkModel(location, job.Description),
            EmploymentType = JobSourceMapping.MapEmploymentType(job.EmploymentType),
            PublishedAt = ParsePublishedAt(job.PublishedOn),
            Tags = new[] { job.Department, job.Function, job.Industry }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };

        posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
        return posting;
    }

    private static DateTimeOffset? ParsePublishedAt(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var publishedAt)
                ? publishedAt
                : null;

    private sealed record AccountResult(bool Succeeded, IReadOnlyCollection<JobPosting> Jobs);

    private sealed class WorkableResponse
    {
        [JsonPropertyName("jobs")]
        public List<WorkableJob> Jobs { get; init; } = [];
    }

    private sealed class WorkableJob
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("employment_type")]
        public string? EmploymentType { get; init; }

        [JsonPropertyName("telecommuting")]
        public bool Telecommuting { get; init; }

        [JsonPropertyName("department")]
        public string? Department { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("published_on")]
        public string? PublishedOn { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("function")]
        public string? Function { get; init; }

        [JsonPropertyName("industry")]
        public string? Industry { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
