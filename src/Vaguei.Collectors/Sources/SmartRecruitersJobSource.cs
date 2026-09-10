using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

public sealed class SmartRecruitersJobSource : IJobSource
{
    private const int PageSize = 100;
    private const int MaximumPagesPerSearchTerm = 3;
    public static readonly IReadOnlyDictionary<string, string> DefaultCompanies =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BoschGroup"] = "Bosch",
            ["SGS"] = "SGS",
            ["Experian"] = "Serasa Experian",
            ["PARSECOPERACOESLOGISTICASLTDA"] = "Parsec Operações Logísticas",
            ["keycommerce"] = "KeyCommerce",
            ["SeInspire"] = "Se Inspire",
            ["adimplere"] = "Adimplere",
            ["PimpMyCarroa"] = "Pimp My Carroça"
        };

    private const string ApiBaseUrl = "https://api.smartrecruiters.com/v1/companies";
    private readonly HttpClient _httpClient;
    private readonly IReadOnlyDictionary<string, string> _companies;
    private readonly int _maximumSearchTerms;
    private readonly TimeSpan _companyTimeout;

    public SmartRecruitersJobSource(
        HttpClient httpClient,
        IReadOnlyDictionary<string, string>? companies = null,
        int maximumSearchTerms = 6,
        TimeSpan? companyTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumSearchTerms);
        _httpClient = httpClient;
        _companies = companies ?? DefaultCompanies;
        _maximumSearchTerms = maximumSearchTerms;
        _companyTimeout = companyTimeout ?? TimeSpan.FromSeconds(10);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            _companyTimeout,
            TimeSpan.Zero);
    }

    public string Name => "SmartRecruiters";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_companies.Select(company =>
            SearchCompanyAsync(company.Key, company.Value, query, cancellationToken)));

        if (results.All(result => !result.Succeeded))
        {
            throw new HttpRequestException(
                "Nenhuma página pública SmartRecruiters pôde ser consultada.");
        }

        return results
            .SelectMany(result => result.Jobs)
            .ToArray();
    }

    private async Task<CompanyResult> SearchCompanyAsync(
        string identifier,
        string company,
        JobSearchQuery query,
        CancellationToken cancellationToken)
    {
        using var companyTimeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        companyTimeout.CancelAfter(_companyTimeout);

        try
        {
            var searchTerms = GetSearchTerms(query, company);
            var listTasks = searchTerms.Select(term =>
                GetSummariesAsync(identifier, term, query, companyTimeout.Token));
            var summaries = (await Task.WhenAll(listTasks))
                .SelectMany(result => result)
                .GroupBy(job => job.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

            using var requestGate = new SemaphoreSlim(6);
            var detailTasks = summaries.Select(async summary =>
            {
                await requestGate.WaitAsync(companyTimeout.Token);
                try
                {
                    return await GetJobAsync(
                        identifier,
                        company,
                        summary,
                        companyTimeout.Token);
                }
                finally
                {
                    requestGate.Release();
                }
            });

            var jobs = (await Task.WhenAll(detailTasks))
                .Where(job => job is not null)
                .Select(job => job!)
                .Where(job => JobSourceMapping.MatchesQuery(job, query))
                .ToArray();

            return new CompanyResult(true, jobs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new CompanyResult(false, []);
        }
    }

    private async Task<IReadOnlyCollection<SmartRecruitersSummary>> GetSummariesAsync(
        string identifier,
        string? searchTerm,
        JobSearchQuery query,
        CancellationToken cancellationToken)
    {
        var summaries = new List<SmartRecruitersSummary>();

        for (var page = 0; page < MaximumPagesPerSearchTerm; page++)
        {
            var parameters = new List<string>
            {
                $"limit={PageSize}",
                $"offset={page * PageSize}"
            };
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                parameters.Add($"q={Uri.EscapeDataString(searchTerm)}");
            }

            if (query.Locations.Any(IsBrazilTerm))
            {
                parameters.Add("country=br");
            }

            var url =
                $"{ApiBaseUrl}/{Uri.EscapeDataString(identifier)}/postings?{string.Join('&', parameters)}";
            var response = await _httpClient.GetFromJsonAsync<SmartRecruitersListResponse>(
                url,
                cancellationToken);
            var pageItems = response?.Content ?? [];
            summaries.AddRange(pageItems);

            if (pageItems.Count < PageSize ||
                (response?.TotalFound is int totalFound && totalFound <= summaries.Count))
            {
                break;
            }
        }

        return summaries;
    }

    private async Task<JobPosting?> GetJobAsync(
        string identifier,
        string company,
        SmartRecruitersSummary summary,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"{ApiBaseUrl}/{Uri.EscapeDataString(identifier)}/postings/{Uri.EscapeDataString(summary.Id)}";
            var job = await _httpClient.GetFromJsonAsync<SmartRecruitersJob>(url, cancellationToken);
            if (job is null)
            {
                return null;
            }

            var description = string.Join(
                ' ',
                new[]
                {
                    job.JobAd?.Sections?.JobDescription?.Text,
                    job.JobAd?.Sections?.Qualifications?.Text,
                    job.JobAd?.Sections?.AdditionalInformation?.Text
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

            var posting = new JobPosting
            {
                Title = job.Name,
                Company = company,
                Description = JobSourceMapping.PlainText(description),
                Location = JobSourceMapping.MapLocation(job.Location?.FullLocation),
                Url = Uri.TryCreate(job.ApplyUrl, UriKind.Absolute, out var uri) ? uri : null,
                Source = Name,
                SourcePostingId = $"{identifier}:{summary.Id}",
                WorkModel = job.Location?.Remote == true
                    ? Vaguei.Domain.Enums.WorkModel.Remote
                    : JobSourceMapping.MapWorkModel(
                        job.Location?.FullLocation,
                        job.Location?.Hybrid == true ? "hybrid" : null),
                EmploymentType = JobSourceMapping.MapEmploymentType(
                    job.TypeOfEmployment?.Label),
                PublishedAt = job.ReleasedDate,
                Tags = new[]
                    {
                        job.Department?.Label,
                        job.Function?.Label,
                        job.Industry?.Label
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
            };

            return posting;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private IReadOnlyCollection<string?> GetSearchTerms(
        JobSearchQuery query,
        string company)
    {
        if (query.Keywords.Count == 0 || query.Keywords.Any(keyword =>
                company.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains(company, StringComparison.OrdinalIgnoreCase)))
        {
            return [null];
        }

        return query.Keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(_maximumSearchTerms)
            .Cast<string?>()
            .ToArray();
    }

    private static bool IsBrazilTerm(string value) =>
        value.Equals("Brasil", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Brazil", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("BR", StringComparison.OrdinalIgnoreCase);

    private sealed record CompanyResult(bool Succeeded, IReadOnlyCollection<JobPosting> Jobs);

    private sealed class SmartRecruitersListResponse
    {
        [JsonPropertyName("content")]
        public List<SmartRecruitersSummary> Content { get; init; } = [];

        [JsonPropertyName("totalFound")]
        public int? TotalFound { get; init; }
    }

    private sealed class SmartRecruitersSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;
    }

    private sealed class SmartRecruitersJob
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("releasedDate")]
        public DateTimeOffset? ReleasedDate { get; init; }

        [JsonPropertyName("location")]
        public SmartRecruitersLocation? Location { get; init; }

        [JsonPropertyName("typeOfEmployment")]
        public SmartRecruitersLabel? TypeOfEmployment { get; init; }

        [JsonPropertyName("department")]
        public SmartRecruitersLabel? Department { get; init; }

        [JsonPropertyName("function")]
        public SmartRecruitersLabel? Function { get; init; }

        [JsonPropertyName("industry")]
        public SmartRecruitersLabel? Industry { get; init; }

        [JsonPropertyName("jobAd")]
        public SmartRecruitersJobAd? JobAd { get; init; }

        [JsonPropertyName("applyUrl")]
        public string? ApplyUrl { get; init; }
    }

    private sealed class SmartRecruitersLocation
    {
        [JsonPropertyName("fullLocation")]
        public string? FullLocation { get; init; }

        [JsonPropertyName("remote")]
        public bool Remote { get; init; }

        [JsonPropertyName("hybrid")]
        public bool Hybrid { get; init; }
    }

    private sealed class SmartRecruitersLabel
    {
        [JsonPropertyName("label")]
        public string? Label { get; init; }
    }

    private sealed class SmartRecruitersJobAd
    {
        [JsonPropertyName("sections")]
        public SmartRecruitersSections? Sections { get; init; }
    }

    private sealed class SmartRecruitersSections
    {
        [JsonPropertyName("jobDescription")]
        public SmartRecruitersText? JobDescription { get; init; }

        [JsonPropertyName("qualifications")]
        public SmartRecruitersText? Qualifications { get; init; }

        [JsonPropertyName("additionalInformation")]
        public SmartRecruitersText? AdditionalInformation { get; init; }
    }

    private sealed class SmartRecruitersText
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }
}
