using Vaguei.Collectors.Configuration;

namespace Vaguei.Tests.Collectors;

public sealed class JobSourceCatalogTests
{
    [Fact]
    public void Load_UsesValidatedConfigurationAndDefaultsForMissingSources()
    {
        var path = CreateTemporaryCatalog(
            """
            {
              "ashby": {
                " example ": " Empresa Exemplo ",
                "": "Inválida"
              }
            }
            """);

        try
        {
            var catalog = JobSourceCatalog.Load(path);

            Assert.Equal("Empresa Exemplo", catalog.Ashby["example"]);
            Assert.Equal(
                JobSourceCatalog.CreateDefault().Greenhouse,
                catalog.Greenhouse);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_FallsBackToDefaultsWhenJsonIsInvalid()
    {
        var path = CreateTemporaryCatalog("{ invalid json");

        try
        {
            var catalog = JobSourceCatalog.Load(path);

            Assert.Equal(
                JobSourceCatalog.CreateDefault().Workable,
                catalog.Workable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ReadsEmbeddedCompatibleStream()
    {
        using var stream = new MemoryStream(
            System.Text.Encoding.UTF8.GetBytes(
                """
                { "inhire": { "sidia": "Sidia" } }
                """));

        var catalog = JobSourceCatalog.Load(stream);

        Assert.Equal("Sidia", catalog.InHire["sidia"]);
        Assert.NotEmpty(catalog.Greenhouse);
    }

    [Fact]
    public void CreateDefault_IncludesValidatedBrazilianEmployers()
    {
        var catalog = JobSourceCatalog.CreateDefault();

        Assert.Equal("CI&T", catalog.Lever["ciandt"]);
        Assert.Equal("dLocal", catalog.Lever["dlocal"]);
        Assert.Equal("Wildlife Studios", catalog.Greenhouse["wildlifestudios"]);
        Assert.Equal("AlphaSights", catalog.Greenhouse["alphasights"]);
        Assert.Equal("Wabtec", catalog.SmartRecruiters["Wabtec"]);
        Assert.Equal("Anglo American", catalog.SmartRecruiters["AngloAmericanDeBeersGroup"]);
        Assert.Equal("Continental", catalog.SmartRecruiters["Continental"]);
    }

    private static string CreateTemporaryCatalog(string contents)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vaguei-job-sources-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        return path;
    }
}
