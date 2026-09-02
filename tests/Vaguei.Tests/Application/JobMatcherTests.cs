using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobMatcherTests
{
    private readonly JobMatcher _matcher =
        new();

    [Fact]
    public void Match_ReturnsMaximumScoreForStrongMatch()
    {
        var profile =
            CreateProfile(
                "Backend Developer",
                "C#",
                ".NET",
                "PostgreSQL",
                "Docker");

        var job =
            CreateJob(
                "Backend Developer",
                """
                Desenvolvimento de APIs usando
                C#, .NET, PostgreSQL e Docker.
                """);

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            100,
            result.Score);
    }

    [Fact]
    public void Match_ReturnsZeroWhenNoEvidenceMatches()
    {
        var profile =
            CreateProfile(
                "Graphic Designer",
                "Photoshop",
                "Illustrator");

        var job =
            CreateJob(
                "Financial Analyst",
                "Excel, accounting and financial reports.");

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            0,
            result.Score);
    }

    [Fact]
    public void Match_ScoresRoleWithoutSkillMatches()
    {
        var profile =
            CreateProfile(
                "Data Analyst",
                "Python",
                "Power BI",
                "SQL",
                "Excel");

        var job =
            CreateJob(
                "Data Analyst",
                "Business reporting position.");

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            50,
            result.Score);
    }

    [Fact]
    public void Match_ScoresSkillsWithoutRoleMatch()
    {
        var profile =
            CreateProfile(
                "Software Developer",
                "C#",
                ".NET",
                "PostgreSQL",
                "Docker");

        var job =
            CreateJob(
                "Technical Specialist",
                """
                C#, .NET, PostgreSQL and Docker
                are required.
                """);

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            50,
            result.Score);
    }

    [Fact]
    public void Match_DoesNotPenalizeProfileWithSingleSkill()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "Excel");

        var job =
            CreateJob(
                "Administrative Assistant",
                "Advanced Excel required.");

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            100,
            result.Score);
    }

    [Fact]
    public void Match_UsesDesiredRoleWhenProvided()
    {
        var profile =
            CreateProfile(
                "Software Developer");

        var preferences =
            new JobSearchPreferences();

        preferences.DesiredRoles.Add(
            "Data Analyst");

        var job =
            CreateJob(
                "Data Analyst",
                "Analytics position.");

        var result =
            _matcher.Match(
                profile,
                job,
                preferences);

        Assert.Equal(
            100,
            result.Score);
    }

    [Fact]
    public void Match_UsesStructuredJobSkills()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "AutoCAD");

        var job =
            CreateJob(
                "Mechanical Designer",
                string.Empty);

        job.Skills.Add(
            "AutoCAD");

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            100,
            result.Score);

        Assert.Contains(
            result.Reasons,
            reason =>
                reason.Criterion ==
                    JobMatchCriterion.Skill &&
                reason.Kind ==
                    JobMatchReasonKind.Positive);
    }

    [Fact]
    public void Match_ReturnsNeutralReasonWhenThereIsNoData()
    {
        var profile =
            CreateProfile(
                string.Empty);

        var job =
            CreateJob(
                "Unknown Position",
                string.Empty);

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            0,
            result.Score);

        Assert.Contains(
            result.Reasons,
            reason =>
                reason.Kind ==
                    JobMatchReasonKind.Neutral);
    }

    [Fact]
    public void Match_RecognizesPortugueseAndEnglishDeveloperRoles()
    {
        var profile =
            CreateProfile(
                "Desenvolvedor .NET");

        var job =
            CreateJob(
                "Software Developer",
                string.Empty);

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            50,
            result.Score);

        Assert.Contains(
            result.Reasons,
            reason =>
                reason.Criterion ==
                JobMatchCriterion.ProfessionalRole &&
                reason.Kind ==
                JobMatchReasonKind.Positive);
    }

    [Fact]
    public void Match_RecognizesEquivalentSoftwareEngineerRole()
    {
        var profile =
            CreateProfile(
                "Engenheiro de Software");

        var job =
            CreateJob(
                "Senior Software Engineer",
                string.Empty);

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            100,
            result.Score);
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

    private static JobPosting CreateJob(
        string title,
        string description)
    {
        return new JobPosting
        {
            Title = title,
            Company = "Test Company",
            Description = description
        };
    }

    [Fact]
    public void Match_GivesMoreWeightToPrimarySkills()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "Primary Skill",
                "Supporting Skill");

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Primary Skill",
                SkillRelevance.Primary));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill",
                SkillRelevance.Supporting));

        var primaryJob =
            CreateJob(
                "Example Job",
                "Experience with Primary Skill.");

        var supportingJob =
            CreateJob(
                "Example Job",
                "Experience with Supporting Skill.");

        var preferences =
            new JobSearchPreferences();

        var primaryResult =
            _matcher.Match(
                profile,
                primaryJob,
                preferences);

        var supportingResult =
            _matcher.Match(
                profile,
                supportingJob,
                preferences);

        Assert.True(
            primaryResult.Score >
            supportingResult.Score);
    }

    [Fact]
    public void Match_UsesWeightedDetailedSkills()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "Primary Skill",
                "Relevant Skill",
                "Supporting Skill");

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Primary Skill",
                SkillRelevance.Primary));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Relevant Skill",
                SkillRelevance.Relevant));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill",
                SkillRelevance.Supporting));

        var job =
            CreateJob(
                "Example Job",
                "Experience with Primary Skill.");

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            62.5,
            result.Score);
    }

    [Fact]
    public void Match_PreservesLegacySkillScoring()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "C#",
                ".NET",
                "PostgreSQL",
                "Git");

        var job =
            CreateJob(
                "Example Job",
                "C# and .NET.");

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            50,
            result.Score);
    }

    [Fact]
    public void Match_DoesNotDiluteScoreBecauseProfileHasManySkills()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "Primary Skill",
                "Relevant Skill 1",
                "Relevant Skill 2",
                "Relevant Skill 3",
                "Supporting Skill 1",
                "Supporting Skill 2",
                "Supporting Skill 3",
                "Supporting Skill 4");

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Primary Skill",
                SkillRelevance.Primary));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Relevant Skill 1",
                SkillRelevance.Relevant));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Relevant Skill 2",
                SkillRelevance.Relevant));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Relevant Skill 3",
                SkillRelevance.Relevant));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 1",
                SkillRelevance.Supporting));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 2",
                SkillRelevance.Supporting));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 3",
                SkillRelevance.Supporting));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 4",
                SkillRelevance.Supporting));

        var job =
            CreateJob(
                "Example Job",
                """
                Primary Skill,
                Relevant Skill 1,
                Relevant Skill 2,
                Relevant Skill 3.
                """);

        var result =
            _matcher.Match(
                profile,
                job,
                new JobSearchPreferences());

        Assert.Equal(
            100,
            result.Score);
    }

    [Fact]
    public void Match_DoesNotLetSupportingSkillsEqualPrimaryEvidence()
    {
        var profile =
            CreateProfile(
                string.Empty,
                "Primary Skill",
                "Relevant Skill 1",
                "Relevant Skill 2",
                "Supporting Skill 1",
                "Supporting Skill 2",
                "Supporting Skill 3",
                "Supporting Skill 4");

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Primary Skill",
                SkillRelevance.Primary));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Relevant Skill 1",
                SkillRelevance.Relevant));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Relevant Skill 2",
                SkillRelevance.Relevant));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 1",
                SkillRelevance.Supporting));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 2",
                SkillRelevance.Supporting));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 3",
                SkillRelevance.Supporting));

        profile.DetailedSkills.Add(
            new CandidateSkill(
                "Supporting Skill 4",
                SkillRelevance.Supporting));

        var primaryJob =
            CreateJob(
                "Example Job",
                "Primary Skill.");

        var supportingJob =
            CreateJob(
                "Example Job",
                """
                Supporting Skill 1,
                Supporting Skill 2,
                Supporting Skill 3,
                Supporting Skill 4.
                """);

        var preferences =
            new JobSearchPreferences();

        var primaryResult =
            _matcher.Match(
                profile,
                primaryJob,
                preferences);

        var supportingResult =
            _matcher.Match(
                profile,
                supportingJob,
                preferences);

        Assert.True(
            primaryResult.Score >
            supportingResult.Score);
    }
}