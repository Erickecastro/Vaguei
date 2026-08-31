using Vaguei.Application.Services;

namespace Vaguei.Tests.Application.Services;

public sealed class WorkExperienceExtractorTests
{
    [Fact]
    public void Extract_ShouldExtractCurrentAndPreviousExperiences()
    {
        var resumeText = string.Join(
            Environment.NewLine,
            [
                "Ericke Castro",
                "Desenvolvedor .NET",
                "",
                "EXPERIÊNCIA PROFISSIONAL",
                "Desenvolvedor de Sistemas",
                "Empresa A | 2026 — Atual",
                "· Desenvolvimento de APIs REST.",
                "· Desenvolvimento com C#.",
                "",
                "Analista de Sistemas",
                "Empresa B | 2022 — 2026",
                "· Monitoramento de sistemas.",
                "",
                "PROJETOS PESSOAIS",
                "Projeto de exemplo"
            ]);

        var extractor = new WorkExperienceExtractor();

        var experiences = extractor
            .Extract(resumeText)
            .ToList();

        Assert.Equal(2, experiences.Count);

        var current = experiences[0];

        Assert.Equal(
            "Desenvolvedor de Sistemas",
            current.Position);

        Assert.Equal(
            "Empresa A",
            current.Company);

        Assert.Equal(2026, current.StartYear);
        Assert.Null(current.EndYear);
        Assert.True(current.IsCurrent);

        Assert.Contains(
            "Desenvolvimento de APIs REST.",
            current.Description);

        Assert.Contains(
            "Desenvolvimento com C#.",
            current.Description);

        var previous = experiences[1];

        Assert.Equal(
            "Analista de Sistemas",
            previous.Position);

        Assert.Equal(
            "Empresa B",
            previous.Company);

        Assert.Equal(2022, previous.StartYear);
        Assert.Equal(2026, previous.EndYear);
        Assert.False(previous.IsCurrent);

        Assert.Contains(
            "Monitoramento de sistemas.",
            previous.Description);
    }

    [Fact]
    public void Extract_ShouldReturnEmptyWhenExperienceSectionDoesNotExist()
    {
        var resumeText = string.Join(
            Environment.NewLine,
            [
                "Ericke Castro",
                "Desenvolvedor .NET",
                "",
                "LINGUAGENS E TECNOLOGIAS",
                "C#, .NET e PostgreSQL"
            ]);

        var extractor = new WorkExperienceExtractor();

        var experiences = extractor.Extract(resumeText);

        Assert.Empty(experiences);
    }

    [Fact]
    public void Extract_ShouldReturnEmptyForEmptyText()
    {
        var extractor = new WorkExperienceExtractor();

        var experiences = extractor.Extract(string.Empty);

        Assert.Empty(experiences);
    }
}