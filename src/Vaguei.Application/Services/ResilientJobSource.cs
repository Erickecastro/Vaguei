using System.Collections.Concurrent;
using Vaguei.Application.Interfaces;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class ResilientJobSource : IJobSource
{
    private readonly IJobSource _inner;
    private readonly SemaphoreSlim _concurrencyGate;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _cacheDuration;
    private readonly int _retryCount;
    private readonly int _maximumCacheEntries;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public ResilientJobSource(
        IJobSource inner,
        SemaphoreSlim concurrencyGate,
        TimeSpan timeout,
        TimeSpan cacheDuration,
        int retryCount = 1,
        int maximumCacheEntries = 32)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(concurrencyGate);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(cacheDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCacheEntries);

        _inner = inner;
        _concurrencyGate = concurrencyGate;
        _timeout = timeout;
        _cacheDuration = cacheDuration;
        _retryCount = retryCount;
        _maximumCacheEntries = maximumCacheEntries;
    }

    public string Name => _inner.Name;

    public async Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var key = CreateCacheKey(query);

        if (TryGetCached(key, out var cachedJobs))
        {
            return cachedJobs;
        }

        await _concurrencyGate.WaitAsync(cancellationToken);

        try
        {
            if (TryGetCached(key, out cachedJobs))
            {
                return cachedJobs;
            }

            for (var attempt = 0; ; attempt++)
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);
                timeoutSource.CancelAfter(_timeout);

                try
                {
                    var jobs = (await _inner.SearchAsync(
                            query,
                            timeoutSource.Token))
                        .ToArray();
                    PruneCache();
                    _cache[key] = new CacheEntry(
                        DateTimeOffset.UtcNow.Add(_cacheDuration),
                        jobs);
                    return jobs;
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    if (attempt >= _retryCount)
                    {
                        throw new TimeoutException(
                            $"A fonte {Name} excedeu o tempo limite de {_timeout.TotalSeconds:0} segundos.");
                    }
                }
                catch (HttpRequestException) when (attempt < _retryCount)
                {
                    // A próxima iteração repete somente falhas transitórias de rede.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
        finally
        {
            _concurrencyGate.Release();
        }
    }

    private void PruneCache()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var expired in _cache.Where(entry => entry.Value.ExpiresAt <= now))
        {
            _cache.TryRemove(expired.Key, out _);
        }

        while (_cache.Count >= _maximumCacheEntries)
        {
            var oldest = _cache.MinBy(entry => entry.Value.ExpiresAt);

            if (oldest.Key is null || !_cache.TryRemove(oldest.Key, out _))
            {
                break;
            }
        }
    }

    private bool TryGetCached(
        string key,
        out IReadOnlyCollection<JobPosting> jobs)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                jobs = entry.Jobs;
                return true;
            }

            _cache.TryRemove(key, out _);
        }

        jobs = [];
        return false;
    }

    private static string CreateCacheKey(JobSearchQuery query) => string.Join(
        '|',
        Normalize(query.Keywords),
        Normalize(query.Locations),
        Normalize(query.WorkModels.Select(value => value.ToString())),
        Normalize(query.EmploymentTypes.Select(value => value.ToString())),
        Normalize(query.SeniorityLevels.Select(value => value.ToString())));

    private static string Normalize(IEnumerable<string> values) => string.Join(
        '\u001f',
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal));

    private sealed record CacheEntry(
        DateTimeOffset ExpiresAt,
        IReadOnlyCollection<JobPosting> Jobs);
}
