using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

/// <summary>
/// Reads job postings from the anonymous, read-only endpoints used by public
/// InHire career pages. It never accesses candidate or administrative data.
/// </summary>
public sealed class InHireJobSource : IJobSource
{
    public static readonly IReadOnlyDictionary<string, string> DefaultTenants =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sidia"] = "Sidia"
        };

    private const string ApiBaseUrl = "https://api.inhire.app";
    private readonly HttpClient _httpClient;
    private readonly IReadOnlyDictionary<string, string> _tenants;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public InHireJobSource(
        HttpClient httpClient,
        IReadOnlyDictionary<string, string>? tenants = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _tenants = tenants ?? DefaultTenants;
    }

    public string Name => "InHire";

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_tenants.Select(tenant =>
            SearchTenantAsync(tenant.Key, tenant.Value, query, cancellationToken)));

        if (results.All(result => !result.Succeeded))
        {
            throw new HttpRequestException("Nenhuma página pública InHire pôde ser consultada.");
        }

        return results.SelectMany(result => result.Jobs).ToArray();
    }

    private async Task<TenantResult> SearchTenantAsync(
        string tenantId,
        string company,
        JobSearchQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            using var listRequest = CreateRequest(
                HttpMethod.Get,
                $"{ApiBaseUrl}/job-posts/public/pages/lean",
                tenantId);
            using var listResponse = await _httpClient.SendAsync(listRequest, cancellationToken);
            listResponse.EnsureSuccessStatusCode();

            var summaries = await listResponse.Content.ReadFromJsonAsync<List<InHireJobSummary>>(
                cancellationToken: cancellationToken) ?? [];

            using var requestGate = new SemaphoreSlim(6);
            var detailTasks = summaries.Select(async summary =>
            {
                await requestGate.WaitAsync(cancellationToken);
                try
                {
                    return await GetJobAsync(tenantId, company, summary, cancellationToken);
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

            return new TenantResult(true, jobs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new TenantResult(false, []);
        }
    }

    private async Task<JobPosting?> GetJobAsync(
        string tenantId,
        string company,
        InHireJobSummary summary,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                $"{ApiBaseUrl}/job-posts/public/pages/{Uri.EscapeDataString(summary.JobId)}",
                tenantId);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var job = await response.Content.ReadFromJsonAsync<InHireJob>(
                cancellationToken: cancellationToken);
            if (job is null || !string.Equals(job.Status, "published", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var posting = new JobPosting
            {
                Title = string.IsNullOrWhiteSpace(job.DisplayName)
                    ? summary.DisplayName
                    : job.DisplayName,
                Company = company,
                Description = JobSourceMapping.PlainText(job.Description),
                Location = JobSourceMapping.MapLocation(job.Location),
                Url = Uri.TryCreate(summary.Link, UriKind.Absolute, out var uri) ? uri : null,
                Source = Name,
                WorkModel = JobSourceMapping.MapWorkModel(job.Location, job.WorkplaceType),
                EmploymentType = JobSourceMapping.MapEmploymentType(
                    string.Join(' ', job.ContractType)),
                PublishedAt = job.LastPublishedAt ?? job.PublishedAt ?? job.CreatedAt,
                Tags = job.ContractType
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
            };

            posting.SkillRequirements = _requirementAnalyzer.Analyze(posting).ToList();
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

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        string tenantId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Tenant", tenantId);
        return request;
    }

    private sealed record TenantResult(bool Succeeded, IReadOnlyCollection<JobPosting> Jobs);

    private sealed class InHireJobSummary
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; init; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("link")]
        public string? Link { get; init; }
    }

    private sealed class InHireJob
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("workplaceType")]
        public string? WorkplaceType { get; init; }

        [JsonPropertyName("contractType")]
        public List<string> ContractType { get; init; } = [];

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset? CreatedAt { get; init; }

        [JsonPropertyName("publishedAt")]
        public DateTimeOffset? PublishedAt { get; init; }

        [JsonPropertyName("lastPublishedAt")]
        public DateTimeOffset? LastPublishedAt { get; init; }
    }
}
