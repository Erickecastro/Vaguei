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
}