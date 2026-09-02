using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class CandidateProfileDetailedSkillsTests
{
    [Fact]
    public void DetailedSkills_StartsEmpty()
    {
        var profile =
            new CandidateProfile();

        Assert.Empty(
            profile.DetailedSkills);
    }

    [Fact]
    public void DetailedSkills_AcceptsCandidateSkills()
    {
        var profile =
            new CandidateProfile();

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "C#",
                SkillRelevance.Primary));

        var skill =
            Assert.Single(
                profile.DetailedSkills);

        Assert.Equal(
            "C#",
            skill.Name);

        Assert.Equal(
            SkillRelevance.Primary,
            skill.Relevance);
    }

    [Fact]
    public void DetailedSkills_CanCoexistWithLegacySkills()
    {
        var profile =
            new CandidateProfile();

        profile.Skills.Add(
            "PostgreSQL");

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "PostgreSQL",
                SkillRelevance.Relevant));

        Assert.Contains(
            "PostgreSQL",
            profile.Skills);

        var detailedSkill =
            Assert.Single(
                profile.DetailedSkills);

        Assert.Equal(
            "PostgreSQL",
            detailedSkill.Name);
    }
}