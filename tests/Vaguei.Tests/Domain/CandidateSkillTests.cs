using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class CandidateSkillTests
{
    [Fact]
    public void Constructor_CreatesCandidateSkill()
    {
        var skill =
            new CandidateSkill(
                "C#",
                SkillRelevance.Primary);

        Assert.Equal(
            "C#",
            skill.Name);

        Assert.Equal(
            SkillRelevance.Primary,
            skill.Relevance);
    }

    [Fact]
    public void Constructor_TrimsSkillName()
    {
        var skill =
            new CandidateSkill(
                "  PostgreSQL  ",
                SkillRelevance.Relevant);

        Assert.Equal(
            "PostgreSQL",
            skill.Name);
    }

    [Fact]
    public void Constructor_PreservesDistinctEvidence()
    {
        var skill = new CandidateSkill(
            "Project management",
            SkillRelevance.Primary,
            [
                new SkillEvidence(SkillEvidenceSource.Project),
                new SkillEvidence(SkillEvidenceSource.Project),
                new SkillEvidence(SkillEvidenceSource.Certification)
            ]);

        Assert.Equal(2, skill.Evidence.Count);
        Assert.Contains(
            skill.Evidence,
            evidence =>
                evidence.Source == SkillEvidenceSource.Project);
    }

    [Theory]
    [InlineData(SkillRelevance.Unspecified)]
    [InlineData(SkillRelevance.Supporting)]
    [InlineData(SkillRelevance.Relevant)]
    [InlineData(SkillRelevance.Primary)]
    public void Constructor_AcceptsAllRelevanceLevels(
        SkillRelevance relevance)
    {
        var skill =
            new CandidateSkill(
                "Example Skill",
                relevance);

        Assert.Equal(
            relevance,
            skill.Relevance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidSkillName(
        string? name)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new CandidateSkill(
                    name!,
                    SkillRelevance.Relevant));
    }
}
