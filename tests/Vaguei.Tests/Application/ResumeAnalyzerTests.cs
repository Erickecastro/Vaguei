using Vaguei.Application.Services;
using Vaguei.Domain.Enums;

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

    [Fact]
    public void Analyze_PopulatesDetailedSkills()
    {
        const string resumeText =
            """
            Maria Silva
            Desenvolvedor .NET

            EXPERIÊNCIA PROFISSIONAL
            Desenvolvedor de Software
            Empresa Exemplo | 2024 — Atual
            Desenvolvimento de aplicações utilizando .NET e PostgreSQL.

            LINGUAGENS E TECNOLOGIAS
            C#, .NET, PostgreSQL, Git
            """;

        var analyzer =
            new ResumeAnalyzer();

        var profile =
            analyzer.Analyze(
                resumeText);

        Assert.NotEmpty(
            profile.DetailedSkills);

        Assert.Contains(
            profile.DetailedSkills,
            skill =>
                skill.Name.Equals(
                    ".NET",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ClassifiesTitleSkillAsPrimary()
    {
        const string resumeText =
            """
            Maria Silva
            Desenvolvedor .NET

            LINGUAGENS E TECNOLOGIAS
            C#, .NET, Git
            """;

        var analyzer =
            new ResumeAnalyzer();

        var profile =
            analyzer.Analyze(
                resumeText);

        var dotnet =
            Assert.Single(
                profile.DetailedSkills,
                skill =>
                    skill.Name.Equals(
                        ".NET",
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            SkillRelevance.Primary,
            dotnet.Relevance);
    }

    [Fact]
    public void Analyze_ClassifiesExperienceSkillAsRelevant()
    {
        const string resumeText =
            """
            Maria Silva
            Desenvolvedor de Software

            EXPERIÊNCIA PROFISSIONAL
            Desenvolvedor de Software
            Empresa Exemplo | 2024 — Atual
            Desenvolvimento de aplicações utilizando PostgreSQL.

            LINGUAGENS E TECNOLOGIAS
            PostgreSQL
            """;

        var analyzer =
            new ResumeAnalyzer();

        var profile =
            analyzer.Analyze(
                resumeText);

        var postgresql =
            Assert.Single(
                profile.DetailedSkills,
                skill =>
                    skill.Name.Equals(
                        "PostgreSQL",
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            SkillRelevance.Primary,
            postgresql.Relevance);

        Assert.Contains(
            postgresql.Evidence,
            evidence =>
                evidence.Source == SkillEvidenceSource.ExperienceDescription);

        Assert.Contains(
            postgresql.Evidence,
            evidence =>
                evidence.Source == SkillEvidenceSource.SkillsSection);
    }

    [Fact]
    public void Analyze_PreservesSkillEvidenceFromGenericSections()
    {
        const string resumeText =
            """
            Maria Silva
            Analista de Sistemas

            PROJETOS
            Aplicação criada com Docker.

            CURSOS
            Fundamentos de Docker.
            """;

        var profile = new ResumeAnalyzer().Analyze(resumeText);

        var docker = Assert.Single(
            profile.DetailedSkills,
            skill => skill.Name == "Docker");

        Assert.Contains(
            docker.Evidence,
            evidence => evidence.Source == SkillEvidenceSource.Project);

        Assert.Contains(
            docker.Evidence,
            evidence => evidence.Source == SkillEvidenceSource.Course);
    }

    [Fact]
    public void Analyze_PreservesEvidenceWhenResumeUsesSkillAlias()
    {
        const string resumeText =
            """
            Maria Silva
            Analista de Dados

            EXPERIÊNCIA PROFISSIONAL
            Analista de Dados
            Empresa Exemplo | 2024 — Atual
            Construção de relatórios com Postgres.
            """;

        var profile = new ResumeAnalyzer().Analyze(resumeText);

        var postgresql = Assert.Single(
            profile.DetailedSkills,
            skill => skill.Name == "PostgreSQL");

        Assert.Contains(
            postgresql.Evidence,
            evidence =>
                evidence.Source == SkillEvidenceSource.ExperienceDescription);
    }

    [Fact]
    public void Analyze_KeepsLegacySkillsAndDetailedSkillsConsistent()
    {
        const string resumeText =
            """
            Maria Silva
            Desenvolvedor .NET

            LINGUAGENS E TECNOLOGIAS
            C#, .NET, PostgreSQL, Git
            """;

        var analyzer =
            new ResumeAnalyzer();

        var profile =
            analyzer.Analyze(
                resumeText);

        Assert.Equal(
            profile.Skills.Count,
            profile.DetailedSkills.Count);

        foreach (var skill in profile.Skills)
        {
            Assert.Contains(
                profile.DetailedSkills,
                detailedSkill =>
                    detailedSkill.Name.Equals(
                        skill,
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
