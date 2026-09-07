using System.Diagnostics;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Models;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobSearchOrchestrator
{
    private readonly IReadOnlyCollection<IJobSource> _sources;
    private readonly JobSearchQueryBuilder _queryBuilder;
    private readonly JobGeographyFilter _geographyFilter;
    private readonly JobFreshnessFilter _freshnessFilter;
    private readonly JobAttributeFilter _attributeFilter;
    private readonly JobDeduplicator _deduplicator;
    private readonly JobMatcher _matcher;
    private readonly JobSkillRequirementAnalyzer _requirementAnalyzer = new();

    public JobSearchOrchestrator(
        IEnumerable<IJobSource> sources)
        : this(
            sources,
            new JobSearchQueryBuilder(),
            new JobGeographyFilter(),
            new JobFreshnessFilter(),
            new JobAttributeFilter(),
            new JobDeduplicator(),
            new JobMatcher())
    {
    }

    public JobSearchOrchestrator(
        IEnumerable<IJobSource> sources,
        JobSearchQueryBuilder queryBuilder,
        JobGeographyFilter geographyFilter,
        JobFreshnessFilter freshnessFilter,
        JobAttributeFilter attributeFilter,
        JobDeduplicator deduplicator,
        JobMatcher matcher)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(queryBuilder);
        ArgumentNullException.ThrowIfNull(geographyFilter);
        ArgumentNullException.ThrowIfNull(freshnessFilter);
        ArgumentNullException.ThrowIfNull(attributeFilter);
        ArgumentNullException.ThrowIfNull(deduplicator);
        ArgumentNullException.ThrowIfNull(matcher);

        _sources = sources.ToArray();
        _queryBuilder = queryBuilder;
        _geographyFilter = geographyFilter;
        _freshnessFilter = freshnessFilter;
        _attributeFilter = attributeFilter;
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

        var sourceTasks = _sources.Select(source =>
                SearchSourceAsync(
                    source,
                    query,
                    cancellationToken))
            .ToArray();

        var completedResults = new List<SourceSearchResult>();
        var pendingTasks = sourceTasks.ToHashSet();

        try
        {
            while (pendingTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(pendingTasks)
                    .WaitAsync(cancellationToken);
                pendingTasks.Remove(completedTask);
                completedResults.Add(await completedTask);
            }
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested &&
            completedResults.Count > 0)
        {
            Console.WriteLine(
                $"[Vaguei.Search] status=partial completedSources={completedResults.Count} pendingSources={pendingTasks.Count}");
        }

        var sourceResults = completedResults.ToArray();

        var collectedJobs = sourceResults
            .SelectMany(result => result.Jobs)
            .ToArray();

        var geographicallyAllowedJobs = _geographyFilter.Filter(
            collectedJobs,
            preferences);

        var freshJobs = _freshnessFilter.Filter(
            geographicallyAllowedJobs,
            preferences,
            referenceTime);

        var attributeAllowedJobs = _attributeFilter.Filter(
            freshJobs,
            preferences);

        var uniqueJobs = _deduplicator.Deduplicate(attributeAllowedJobs);

        foreach (var job in uniqueJobs)
        {
            if (job.SkillRequirements.Count == 0)
            {
                job.SkillRequirements =
                    _requirementAnalyzer.Analyze(job).ToList();
            }
        }

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
            SourceSummaries = sourceResults
                .Select(result => new JobSourceSearchSummary(
                    result.Source,
                    result.Jobs.Count,
                    result.Failure is null))
                .ToArray(),
            CollectedJobCount = collectedJobs.Length,
            UniqueJobCount = uniqueJobs.Count,
            AllSourcesFailed = sourceResults.Length > 0 &&
                               sourceResults.All(result => result.Failure is not null)
        };
    }

    private static async Task<SourceSearchResult> SearchSourceAsync(
        IJobSource source,
        JobSearchQuery query,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var jobs = await source.SearchAsync(
                query,
                cancellationToken);

            var materializedJobs = jobs.ToArray();
            Console.WriteLine(
                $"[Vaguei.Search] source={source.Name} status=ok jobs={materializedJobs.Length} elapsedMs={stopwatch.ElapsedMilliseconds}");

            return new SourceSearchResult(
                source.Name,
                materializedJobs,
                null);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"[Vaguei.Search] source={source.Name} status=failed type={exception.GetType().Name} elapsedMs={stopwatch.ElapsedMilliseconds}");

            return new SourceSearchResult(
                source.Name,
                [],
                new JobSourceFailure(
                    source.Name,
                    exception.Message));
        }
    }

    private sealed record SourceSearchResult(
        string Source,
        IReadOnlyCollection<JobPosting> Jobs,
        JobSourceFailure? Failure);
}
