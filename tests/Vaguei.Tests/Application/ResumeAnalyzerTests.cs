using Vaguei.Application.Services;

namespace Vaguei.Tests.Application;

public sealed class ResumeAnalyzerTests
{
    [Fact]
    public void Analyze_WithValidResume_ReturnsProfile()
    {
        var analyzer =
            new ResumeAnalyzer();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "RESUMO PROFISSIONAL",
            "Desenvolvedor com experiência em APIs REST.",
            "EXPERIÊNCIA PROFISSIONAL",
            "Desenvolvedor Backend",
            "Empresa Teste | 2024 — Atual",
            "Desenvolvimento de APIs REST com C# e ASP.NET Core.",
            "LINGUAGENS E TECNOLOGIAS",
            "C#, ASP.NET Core, PostgreSQL");

        var result =
            analyzer.Analyze(resume);

        Assert.Equal(
            "Pessoa Teste",
            result.Name);

        Assert.Equal(
            "Desenvolvedor Backend",
            result.ProfessionalTitle);

        Assert.Equal(
            "Desenvolvedor com experiência em APIs REST.",
            result.Summary);

        Assert.Contains(
            "C#",
            result.Skills);

        Assert.Contains(
            "ASP.NET Core",
            result.Skills);

        Assert.Contains(
            "PostgreSQL",
            result.Skills);
    }

    [Fact]
    public void Analyze_WithExperience_ReturnsExperience()
    {
        var analyzer =
            new ResumeAnalyzer();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "EXPERIÊNCIA PROFISSIONAL",
            "Desenvolvedor Backend",
            "Empresa Teste | 2023 — 2025",
            "Desenvolvimento de aplicações com C#.",
            "FORMAÇÃO ACADÊMICA",
            "Ciência da Computação");

        var result =
            analyzer.Analyze(resume);

        Assert.Single(
            result.Experiences);

        var experience =
            result.Experiences[0];

        Assert.Equal(
            "Desenvolvedor Backend",
            experience.Position);

        Assert.Equal(
            "Empresa Teste",
            experience.Company);

        Assert.Equal(
            2023,
            experience.StartYear);

        Assert.Equal(
            2025,
            experience.EndYear);
    }

    [Fact]
    public void Analyze_WithoutSummary_ReturnsEmptySummary()
    {
        var analyzer =
            new ResumeAnalyzer();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "EXPERIÊNCIA PROFISSIONAL");

        var result =
            analyzer.Analyze(resume);

        Assert.Equal(
            string.Empty,
            result.Summary);
    }

    [Fact]
    public void Analyze_WithKnownSkills_ReturnsSkills()
    {
        var analyzer =
            new ResumeAnalyzer();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor .NET",
            "C#",
            "ASP.NET Core",
            "PostgreSQL",
            "Docker");

        var result =
            analyzer.Analyze(resume);

        Assert.Contains(
            "C#",
            result.Skills);

        Assert.Contains(
            "ASP.NET Core",
            result.Skills);

        Assert.Contains(
            "PostgreSQL",
            result.Skills);

        Assert.Contains(
            "Docker",
            result.Skills);
    }

    [Fact]
    public void Analyze_WithUnknownSkill_DoesNotAddSkill()
    {
        var analyzer =
            new ResumeAnalyzer();

        var resume = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "TecnologiaInexistente123");

        var result =
            analyzer.Analyze(resume);

        Assert.DoesNotContain(
            "TecnologiaInexistente123",
            result.Skills);
    }

    [Fact]
    public void Analyze_WithEmptyText_ThrowsArgumentException()
    {
        var analyzer =
            new ResumeAnalyzer();

        var action = () =>
            analyzer.Analyze(string.Empty);

        Assert.Throws<ArgumentException>(
            action);
    }
}