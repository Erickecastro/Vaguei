using Vaguei.Application.Interfaces;
using Vaguei.Application.Models;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobSearchOrchestrator
{
    private readonly IReadOnlyCollection<IJobSource> _sources;
    private readonly JobSearchQueryBuilder _queryBuilder;
    private readonly JobFreshnessFilter _freshnessFilter;
    private readonly JobDeduplicator _deduplicator;
    private readonly JobMatcher _matcher;

    public JobSearchOrchestrator(
        IEnumerable<IJobSource> sources)
        : this(
            sources,
            new JobSearchQueryBuilder(),
            new JobFreshnessFilter(),
            new JobDeduplicator(),
            new JobMatcher())
    {
    }

    public JobSearchOrchestrator(
        IEnumerable<IJobSource> sources,
        JobSearchQueryBuilder queryBuilder,
        JobFreshnessFilter freshnessFilter,
        JobDeduplicator deduplicator,
        JobMatcher matcher)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(queryBuilder);
        ArgumentNullException.ThrowIfNull(freshnessFilter);
        ArgumentNullException.ThrowIfNull(deduplicator);
        ArgumentNullException.ThrowIfNull(matcher);

        _sources = sources.ToArray();
        _queryBuilder = queryBuilder;
        _freshnessFilter = freshnessFilter;
        _deduplicator = deduplicator;
        _matcher = matcher;
    }

    public async Task<JobSearchExecutionResult> SearchAsync(
        CandidateProfile profile,
        JobSearchPreferences preferences,
        DateTimeOffset referenceTime,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(preferences);

        var query = _queryBuilder.Build(
            profile,
            preferences);

        var sourceResults = await Task.WhenAll(
            _sources.Select(source =>
                SearchSourceAsync(
                    source,
                    query,
                    cancellationToken)));

        var collectedJobs = sourceResults
            .SelectMany(result => result.Jobs)
            .ToArray();

        var freshJobs = _freshnessFilter.Filter(
            collectedJobs,
            preferences,
            referenceTime);

        var uniqueJobs = _deduplicator.Deduplicate(
            freshJobs);

        var matches = uniqueJobs
            .Select(job =>
                _matcher.Match(
                    profile,
                    job,
                    preferences))
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Job.PublishedAt)
            .ToArray();

        return new JobSearchExecutionResult
        {
            Query = query,
            Matches = matches,
            SourceFailures = sourceResults
                .Where(result => result.Failure is not null)
                .Select(result => result.Failure!)
                .ToArray(),
            CollectedJobCount = collectedJobs.Length,
            UniqueJobCount = uniqueJobs.Count
        };
    }

    private static async Task<SourceSearchResult> SearchSourceAsync(
        IJobSource source,
        JobSearchQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var jobs = await source.SearchAsync(
                query,
                cancellationToken);

            return new SourceSearchResult(
                jobs.ToArray(),
                null);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new SourceSearchResult(
                [],
                new JobSourceFailure(
                    source.Name,
                    exception.Message));
        }
    }

    private sealed record SourceSearchResult(
        IReadOnlyCollection<JobPosting> Jobs,
        JobSourceFailure? Failure);
}
