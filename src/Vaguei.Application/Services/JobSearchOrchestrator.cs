using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        // Algumas fontes consultam cache persistente antes do primeiro await.
        // Em clientes MAUI isso não pode ocupar a thread visual: a leitura,
        // desserialização, coleta e normalização permanecem em segundo plano,
        // enquanto a interface continua responsiva.
        var sourceTasks = _sources.Select(source =>
                Task.Run(
                    () => SearchSourceAsync(
                        source,
                        query,
                        cancellationToken)))
            .ToArray();

        var completedResults = new List<SourceSearchResult>();
        var pendingTasks = sourceTasks.ToHashSet();

        try
        {
            while (pendingTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(pendingTasks)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                pendingTasks.Remove(completedTask);
                completedResults.Add(await completedTask.ConfigureAwait(false));
            }
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested &&
            completedResults.Count > 0)
        {
            Console.WriteLine(
                $"[Vaguei.Search] status=partial completedSources={completedResults.Count} pendingSources={pendingTasks.Count}");
        }

        return BuildExecutionResult(
            query,
            completedResults,
            profile,
            preferences,
            referenceTime);
    }

    public async IAsyncEnumerable<JobSearchExecutionResult> SearchProgressivelyAsync(
        CandidateProfile profile,
        JobSearchPreferences preferences,
        DateTimeOffset referenceTime,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(preferences);

        var query = _queryBuilder.Build(profile, preferences);
        var pendingTasks = _sources.Select(source =>
                Task.Run(() => SearchSourceAsync(source, query, cancellationToken)))
            .ToHashSet();
        var completedResults = new List<SourceSearchResult>();
        var cancelled = false;
        var initialCoveragePublished = false;

        while (pendingTasks.Count > 0)
        {
            Task<SourceSearchResult> completedTask;
            try
            {
                completedTask = await Task.WhenAny(pendingTasks)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            pendingTasks.Remove(completedTask);
            completedResults.Add(await completedTask.ConfigureAwait(false));

            // Enquanto ainda não há uma lista útil, cada fonte concluída pode
            // ajudar a alcançá-la. Depois disso, evitamos normalizar e pontuar
            // toda a coleção repetidamente: basta publicar a consolidação no
            // término da coleta.
            if (!initialCoveragePublished || pendingTasks.Count == 0)
            {
                var result = BuildExecutionResult(
                    query,
                    completedResults,
                    profile,
                    preferences,
                    referenceTime);

                yield return result;

                initialCoveragePublished = result.Matches.Count >= 5 ||
                                         completedResults.Count >= 2 ||
                                         pendingTasks.Count == 0;
            }
        }

        if (cancelled && completedResults.Count > 0)
        {
            yield return BuildExecutionResult(
                query,
                completedResults,
                profile,
                preferences,
                referenceTime);
        }
    }

    private JobSearchExecutionResult BuildExecutionResult(
        JobSearchQuery query,
        IReadOnlyCollection<SourceSearchResult> sourceResults,
        CandidateProfile profile,
        JobSearchPreferences preferences,
        DateTimeOffset referenceTime)
    {

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

        var uniqueJobs = _deduplicator.Deduplicate(attributeAllowedJobs).ToArray();

        Parallel.ForEach(uniqueJobs, job =>
        {
            if (job.SkillRequirements.Count == 0)
            {
                job.SkillRequirements =
                    _requirementAnalyzer.Analyze(job).ToList();
            }
        });

        var matches = uniqueJobs
            .AsParallel()
            .Select(job =>
                _matcher.Match(
                    profile,
                    job,
                    preferences))
            .ToArray()
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
            UniqueJobCount = uniqueJobs.Length,
            AllSourcesFailed = sourceResults.Count > 0 &&
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
