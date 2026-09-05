using Vaguei.Infrastructure;

namespace Vaguei.Tests.Infrastructure;

public sealed class JsonThemePreferenceStoreTests
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void SaveAndLoad_RoundTripsTheme(string theme)
    {
        var path = TemporaryPath();
        try
        {
            var store = new JsonThemePreferenceStore(path);
            store.Save(theme);

            Assert.Equal(theme, store.Load());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidContent_ReturnsNoPreference()
    {
        var path = TemporaryPath();
        try
        {
            File.WriteAllText(path, "invalid");
            Assert.Null(new JsonThemePreferenceStore(path).Load());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static string TemporaryPath() => Path.Combine(
        Path.GetTempPath(),
        $"vaguei-theme-{Guid.NewGuid():N}.json");
}
