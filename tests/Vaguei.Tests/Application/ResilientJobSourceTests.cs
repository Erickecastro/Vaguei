using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class ResilientJobSourceTests
{
    [Fact]
    public async Task SearchAsync_ReusesCachedResultsForEquivalentQuery()
    {
        var inner = new CountingSource();
        var source = CreatePolicy(inner);

        var first = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = [".NET", "C#"]
        });
        var second = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["c#", ".net"]
        });

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_RetriesOneTransientNetworkFailure()
    {
        var inner = new CountingSource(failFirstRequest: true);
        var source = CreatePolicy(inner);

        var jobs = await source.SearchAsync(new JobSearchQuery());

        Assert.Single(jobs);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_EvictsOldestEntryAtConfiguredLimit()
    {
        var inner = new CountingSource();
        var source = new ResilientJobSource(
            inner,
            new SemaphoreSlim(1, 1),
            timeout: TimeSpan.FromSeconds(2),
            cacheDuration: TimeSpan.FromMinutes(1),
            maximumCacheEntries: 2);

        await source.SearchAsync(Query("primeira"));
        await source.SearchAsync(Query("segunda"));
        await source.SearchAsync(Query("terceira"));
        await source.SearchAsync(Query("primeira"));

        Assert.Equal(4, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_DoesNotWaitForPersistentCacheWrite()
    {
        using var cache = new BlockingPersistentCache();
        var source = new ResilientJobSource(
            new CountingSource(),
            new SemaphoreSlim(1, 1),
            timeout: TimeSpan.FromSeconds(2),
            cacheDuration: TimeSpan.FromMinutes(1),
            persistentCache: cache);

        var search = source.SearchAsync(new JobSearchQuery { Keywords = [".NET"] });

        await cache.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var jobs = await search.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Single(jobs);
        cache.AllowSave.Set();
        await cache.SaveFinished.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private static JobSearchQuery Query(string keyword) => new()
    {
        Keywords = [keyword]
    };

    private static ResilientJobSource CreatePolicy(IJobSource source) => new(
        source,
        new SemaphoreSlim(1, 1),
        timeout: TimeSpan.FromSeconds(2),
        cacheDuration: TimeSpan.FromMinutes(1));

    private sealed class CountingSource(bool failFirstRequest = false) : IJobSource
    {
        public int CallCount { get; private set; }

        public string Name => "Teste";

        public Task<IEnumerable<JobPosting>> SearchAsync(
            JobSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (failFirstRequest && CallCount == 1)
            {
                throw new HttpRequestException("Falha transitória.");
            }

            return Task.FromResult<IEnumerable<JobPosting>>(
                [new JobPosting { Title = "Vaga", Company = "Empresa" }]);
        }
    }

    private sealed class BlockingPersistentCache : IPersistentJobCache, IDisposable
    {
        public TaskCompletionSource SaveStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SaveFinished { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim AllowSave { get; } = new(false);

        public bool TryLoad(
            string source,
            string queryKey,
            out IReadOnlyCollection<JobPosting> jobs,
            out DateTimeOffset expiresAt)
        {
            jobs = [];
            expiresAt = default;
            return false;
        }

        public void Save(
            string source,
            string queryKey,
            DateTimeOffset expiresAt,
            IReadOnlyCollection<JobPosting> jobs)
        {
            SaveStarted.TrySetResult();
            try
            {
                AllowSave.Wait(TimeSpan.FromSeconds(2));
            }
            finally
            {
                SaveFinished.TrySetResult();
            }
        }

        public void Dispose() => AllowSave.Dispose();
    }
}
