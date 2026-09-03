using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;

namespace Vaguei.Tests.Application;

public sealed class JobSkillRequirementAnalyzerTests
{
    private readonly JobSkillRequirementAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_ClassifiesSkillInTitleAsCore()
    {
        var job = CreateJob(
            ".NET Developer",
            "Software development position.");

        var requirement = Assert.Single(
            _analyzer.Analyze(job),
            item => item.Name == ".NET");

        Assert.Equal(
            JobSkillRequirementLevel.Core,
            requirement.Level);
    }

    [Theory]
    [InlineData("C# é obrigatório para esta posição.")]
    [InlineData("C# is required for this position.")]
    [InlineData("Must have: C#.")]
    public void Analyze_ClassifiesRequiredSkill(
        string description)
    {
        var job = CreateJob(
            "Software Developer",
            description);

        var requirement = Assert.Single(
            _analyzer.Analyze(job),
            item => item.Name == "C#");

        Assert.Equal(
            JobSkillRequirementLevel.Required,
            requirement.Level);
    }

    [Theory]
    [InlineData("Docker é desejável.")]
    [InlineData("Nice to have: Docker.")]
    public void Analyze_ClassifiesPreferredSkill(
        string description)
    {
        var job = CreateJob(
            "Software Developer",
            description);

        var requirement = Assert.Single(
            _analyzer.Analyze(job),
            item => item.Name == "Docker");

        Assert.Equal(
            JobSkillRequirementLevel.Preferred,
            requirement.Level);
    }

    [Fact]
    public void Analyze_ClassifiesSkillWithoutMarkerAsMentioned()
    {
        var job = CreateJob(
            "Data Analyst",
            "Criação de consultas utilizando SQL.");

        var requirement = Assert.Single(
            _analyzer.Analyze(job),
            item => item.Name == "SQL");

        Assert.Equal(
            JobSkillRequirementLevel.Mentioned,
            requirement.Level);
    }

    [Fact]
    public void Analyze_DoesNotApplyMarkerFromAnotherContext()
    {
        var job = CreateJob(
            "Software Developer",
            "Experiência com C#. Docker é obrigatório.");

        var requirement = Assert.Single(
            _analyzer.Analyze(job),
            item => item.Name == "C#");

        Assert.Equal(
            JobSkillRequirementLevel.Mentioned,
            requirement.Level);
    }

    [Fact]
    public void Analyze_PreservesUnknownStructuredSkill()
    {
        var job = CreateJob(
            "Especialista",
            string.Empty);

        job.Skills.Add("Competência Setorial");

        var requirement = Assert.Single(
            _analyzer.Analyze(job));

        Assert.Equal(
            "Competência Setorial",
            requirement.Name);

        Assert.Equal(
            JobSkillRequirementLevel.Mentioned,
            requirement.Level);
    }

    [Fact]
    public void Analyze_NormalizesKnownStructuredSkillAlias()
    {
        var job = CreateJob(
            "Especialista de Dados",
            string.Empty);

        job.Skills.Add("Postgres");

        var requirement = Assert.Single(
            _analyzer.Analyze(job));

        Assert.Equal(
            "PostgreSQL",
            requirement.Name);
    }

    private static JobPosting CreateJob(
        string title,
        string description)
    {
        return new JobPosting
        {
            Title = title,
            Company = "Empresa Teste",
            Description = description
        };
    }
}
