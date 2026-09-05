using Vaguei.Infrastructure;

namespace Vaguei.Tests.Infrastructure;

public sealed class JsonFavoriteJobStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsFavoriteKeys()
    {
        var path = TemporaryPath();
        try
        {
            var store = new JsonFavoriteJobStore(path);
            store.Save(new HashSet<string> { "Workable:empresa:123", "Lever:site:456" });

            var loaded = store.Load();

            Assert.Equal(2, loaded.Count);
            Assert.Contains("workable:EMPRESA:123", loaded);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidJson_ReturnsEmptySet()
    {
        var path = TemporaryPath();
        try
        {
            File.WriteAllText(path, "invalid json");
            Assert.Empty(new JsonFavoriteJobStore(path).Load());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static string TemporaryPath() => Path.Combine(
        Path.GetTempPath(),
        $"vaguei-favorites-{Guid.NewGuid():N}.json");
}
