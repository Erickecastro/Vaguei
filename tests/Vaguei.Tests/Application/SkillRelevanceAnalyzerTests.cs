using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class SkillRelevanceAnalyzerTests
{
    private readonly SkillRelevanceAnalyzer
        _analyzer =
            new();

    [Fact]
    public void Analyze_ClassifiesTitleSkillAsPrimary()
    {
        var profile =
            CreateProfile(
                "Desenvolvedor .NET",
                ".NET");

        var result =
            _analyzer.Analyze(
                profile);

        var skill =
            Assert.Single(result);

        Assert.Equal(
            ".NET",
            skill.Name);

        Assert.Equal(
            SkillRelevance.Primary,
            skill.Relevance);
    }

    [Fact]
    public void Analyze_ClassifiesExperienceSkillAsRelevant()
    {
        var profile =
            CreateProfile(
                "Software Developer",
                "PostgreSQL");

        profile.Experiences.Add(
            new WorkExperience
            {
                Position =
                    "Software Developer",

                Description =
                    "Desenvolvimento de aplicações com PostgreSQL."
            });

        var result =
            _analyzer.Analyze(
                profile);

        var skill =
            Assert.Single(result);

        Assert.Equal(
            SkillRelevance.Relevant,
            skill.Relevance);
    }

    [Fact]
    public void Analyze_ClassifiesRepeatedEvidenceAsPrimary()
    {
        var profile =
            CreateProfile(
                "Software Developer",
                "Docker");

        profile.Summary =
            "Experiência com Docker.";

        profile.Experiences.Add(
            new WorkExperience
            {
                Description =
                    "Uso de Docker em ambientes de desenvolvimento."
            });

        var result =
            _analyzer.Analyze(
                profile);

        var skill =
            Assert.Single(result);

        Assert.Equal(
            SkillRelevance.Primary,
            skill.Relevance);
    }

    [Fact]
    public void Analyze_ClassifiesUnreferencedSkillAsSupporting()
    {
        var profile =
            CreateProfile(
                "Software Developer",
                "Git");

        var result =
            _analyzer.Analyze(
                profile);

        var skill =
            Assert.Single(result);

        Assert.Equal(
            SkillRelevance.Supporting,
            skill.Relevance);
    }

    [Fact]
    public void Analyze_IsCaseInsensitive()
    {
        var profile =
            CreateProfile(
                "Data Analyst",
                "Power BI");

        profile.Experiences.Add(
            new WorkExperience
            {
                Description =
                    "Criação de dashboards utilizando POWER BI."
            });

        var result =
            _analyzer.Analyze(
                profile);

        var skill =
            Assert.Single(result);

        Assert.Equal(
            SkillRelevance.Relevant,
            skill.Relevance);
    }

    [Fact]
    public void Analyze_AnalyzesMultipleSkillsIndependently()
    {
        var profile =
            CreateProfile(
                "Backend Developer .NET",
                ".NET",
                "PostgreSQL",
                "Git");

        profile.Experiences.Add(
            new WorkExperience
            {
                Description =
                    "Persistência de dados utilizando PostgreSQL."
            });

        var result =
            _analyzer
                .Analyze(profile)
                .ToDictionary(
                    skill => skill.Name,
                    StringComparer.OrdinalIgnoreCase);

        Assert.Equal(
            SkillRelevance.Primary,
            result[".NET"].Relevance);

        Assert.Equal(
            SkillRelevance.Relevant,
            result["PostgreSQL"].Relevance);

        Assert.Equal(
            SkillRelevance.Supporting,
            result["Git"].Relevance);
    }

    [Fact]
    public void Analyze_UsesSectionEvidenceAndPreservesIt()
    {
        var profile = CreateProfile(
            "Analista",
            "Power BI");

        var evidence =
            new Dictionary<string, IReadOnlyCollection<SkillEvidence>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Power BI"] =
                [
                    new SkillEvidence(
                        SkillEvidenceSource.SkillsSection)
                ]
            };

        var result = Assert.Single(
            _analyzer.Analyze(
                profile,
                evidence));

        Assert.Equal(
            SkillRelevance.Relevant,
            result.Relevance);

        Assert.Contains(
            result.Evidence,
            item =>
                item.Source == SkillEvidenceSource.SkillsSection);
    }

    [Fact]
    public void Analyze_CombinesIndependentEvidenceAsPrimary()
    {
        var profile = CreateProfile(
            "Analista",
            "Gestão de Projetos");

        var evidence =
            new Dictionary<string, IReadOnlyCollection<SkillEvidence>>
            {
                ["Gestão de Projetos"] =
                [
                    new SkillEvidence(
                        SkillEvidenceSource.SkillsSection),
                    new SkillEvidence(
                        SkillEvidenceSource.Project)
                ]
            };

        var result = Assert.Single(
            _analyzer.Analyze(
                profile,
                evidence));

        Assert.Equal(
            SkillRelevance.Primary,
            result.Relevance);
    }

    private static CandidateProfile CreateProfile(
        string professionalTitle,
        params string[] skills)
    {
        var profile =
            new CandidateProfile
            {
                ProfessionalTitle =
                    professionalTitle
            };

        foreach (var skill in skills)
        {
            profile.Skills.Add(
                skill);
        }

        return profile;
    }
}
