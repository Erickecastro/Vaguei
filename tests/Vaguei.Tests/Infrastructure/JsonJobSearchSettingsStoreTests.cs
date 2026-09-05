using Vaguei.Application.Models;
using Vaguei.Infrastructure;

namespace Vaguei.Tests.Infrastructure;

public sealed class JsonJobSearchSettingsStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsSearchSettings()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vaguei-settings-{Guid.NewGuid():N}.json");

        try
        {
            var store = new JsonJobSearchSettingsStore(path);
            store.Save(new JobSearchSettings
            {
                SearchScopeIndex = 1,
                PublicationWindowIndex = 4,
                WorkModelIndex = 2,
                EmploymentTypeIndex = 6,
                SeniorityIndex = 1,
                LocationFilter = "Manaus"
            });

            var loaded = store.Load();

            Assert.Equal(1, loaded.SearchScopeIndex);
            Assert.Equal(4, loaded.PublicationWindowIndex);
            Assert.Equal(2, loaded.WorkModelIndex);
            Assert.Equal(6, loaded.EmploymentTypeIndex);
            Assert.Equal(1, loaded.SeniorityIndex);
            Assert.Equal("Manaus", loaded.LocationFilter);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vaguei-settings-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, "invalid json");
            var loaded = new JsonJobSearchSettingsStore(path).Load();

            Assert.Equal(3, loaded.PublicationWindowIndex);
            Assert.Empty(loaded.LocationFilter);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
