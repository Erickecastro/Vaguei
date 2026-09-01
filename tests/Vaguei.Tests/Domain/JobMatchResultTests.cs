using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class JobMatchResultTests
{
    [Fact]
    public void Constructor_CreatesMatchResult()
    {
        var job =
            CreateJob();

        var reasons =
            new[]
            {
                new JobMatchReason
                {
                    Criterion =
                        JobMatchCriterion.ProfessionalRole,

                    Kind =
                        JobMatchReasonKind.Positive,

                    Description =
                        "Cargo compatível com o perfil."
                }
            };

        var result =
            new JobMatchResult(
                job,
                85,
                reasons);

        Assert.Same(
            job,
            result.Job);

        Assert.Equal(
            85,
            result.Score);

        Assert.Single(
            result.Reasons);
    }

    [Fact]
    public void Constructor_AllowsZeroScore()
    {
        var result =
            new JobMatchResult(
                CreateJob(),
                0,
                []);

        Assert.Equal(
            0,
            result.Score);
    }

    [Fact]
    public void Constructor_AllowsMaximumScore()
    {
        var result =
            new JobMatchResult(
                CreateJob(),
                100,
                []);

        Assert.Equal(
            100,
            result.Score);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.1)]
    public void Constructor_RejectsInvalidScore(
        double score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new JobMatchResult(
                    CreateJob(),
                    score,
                    []));
    }

    [Fact]
    public void Constructor_RejectsNullJob()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new JobMatchResult(
                    null!,
                    50,
                    []));
    }

    [Fact]
    public void Constructor_RejectsNullReasons()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new JobMatchResult(
                    CreateJob(),
                    50,
                    null!));
    }

    [Fact]
    public void Constructor_CopiesReasonsCollection()
    {
        var reasons =
            new List<JobMatchReason>
            {
                new()
                {
                    Criterion =
                        JobMatchCriterion.Skill,

                    Kind =
                        JobMatchReasonKind.Positive,

                    Description =
                        "Competência compatível."
                }
            };

        var result =
            new JobMatchResult(
                CreateJob(),
                75,
                reasons);

        reasons.Clear();

        Assert.Single(
            result.Reasons);
    }

    private static JobPosting CreateJob()
    {
        return new JobPosting
        {
            Title = "Test Job",
            Company = "Test Company"
        };
    }
}