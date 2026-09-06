using Vaguei.Domain.Entities;
using Vaguei.Infrastructure;

namespace Vaguei.Tests.Infrastructure;

public sealed class JsonPersistentJobCacheTests
{
    [Fact]
    public void SaveAndTryLoad_RestoresUnexpiredJobsAcrossInstances()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            new JsonPersistentJobCache(directory).Save(
                "Fonte",
                "consulta",
                DateTimeOffset.UtcNow.AddMinutes(5),
                [new JobPosting { Title = "Vaga", Company = "Empresa" }]);

            var loaded = new JsonPersistentJobCache(directory).TryLoad(
                "Fonte",
                "consulta",
                out var jobs,
                out var expiresAt);

            Assert.True(loaded);
            var job = Assert.Single(jobs);
            Assert.Equal("Vaga", job.Title);
            Assert.True(expiresAt > DateTimeOffset.UtcNow);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TryLoad_RejectsExpiredEntry()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var cache = new JsonPersistentJobCache(directory);
            cache.Save(
                "Fonte",
                "consulta",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                [new JobPosting { Title = "Vaga", Company = "Empresa" }]);

            Assert.False(cache.TryLoad(
                "Fonte",
                "consulta",
                out var jobs,
                out _));
            Assert.Empty(jobs);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vaguei-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
