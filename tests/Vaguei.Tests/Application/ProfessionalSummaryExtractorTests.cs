using Vaguei.Application.Services;

namespace Vaguei.Tests.Application;

public sealed class ProfessionalSummaryExtractorTests
{
    [Fact]
    public void Extract_WithProfessionalSummary_ReturnsSummary()
    {
        var extractor =
            new ProfessionalSummaryExtractor();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "RESUMO PROFISSIONAL",
            "Desenvolvedor com experiência em APIs REST,",
            "aplicações web e bancos de dados.",
            "EXPERIÊNCIA PROFISSIONAL",
            "Desenvolvedor Backend");

        var result =
            extractor.Extract(resume);

        Assert.Equal(
            "Desenvolvedor com experiência em APIs REST, aplicações web e bancos de dados.",
            result);
    }

    [Fact]
    public void Extract_WithProfileSection_ReturnsSummary()
    {
        var extractor =
            new ProfessionalSummaryExtractor();

        var resume = string.Join(
            Environment.NewLine,
            "PERFIL PROFISSIONAL",
            "Profissional com experiência em desenvolvimento de software.",
            "FORMAÇÃO ACADÊMICA",
            "Ciência da Computação");

        var result =
            extractor.Extract(resume);

        Assert.Equal(
            "Profissional com experiência em desenvolvimento de software.",
            result);
    }

    [Fact]
    public void Extract_WithoutSummarySection_ReturnsEmptyString()
    {
        var extractor =
            new ProfessionalSummaryExtractor();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "EXPERIÊNCIA PROFISSIONAL",
            "Desenvolvedor Backend");

        var result =
            extractor.Extract(resume);

        Assert.Equal(
            string.Empty,
            result);
    }

    [Fact]
    public void Extract_WithEmptyText_ReturnsEmptyString()
    {
        var extractor =
            new ProfessionalSummaryExtractor();

        var result =
            extractor.Extract(string.Empty);

        Assert.Equal(
            string.Empty,
            result);
    }

    [Fact]
    public void Extract_IsCaseInsensitive()
    {
        var extractor =
            new ProfessionalSummaryExtractor();

        var resume = string.Join(
            Environment.NewLine,
            "resumo profissional",
            "Desenvolvedor de software.",
            "experiência profissional");

        var result =
            extractor.Extract(resume);

        Assert.Equal(
            "Desenvolvedor de software.",
            result);
    }
}