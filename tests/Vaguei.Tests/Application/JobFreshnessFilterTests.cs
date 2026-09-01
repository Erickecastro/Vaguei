using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobFreshnessFilterTests
{
    private static readonly DateTimeOffset ReferenceTime =
        new(
            2026,
            9,
            1,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void IsAllowed_AcceptsJobFromLast24Hours()
    {
        var job =
            CreateJob(
                ReferenceTime.AddHours(-5));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last24Hours,
                ReferenceTime);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_RejectsJobOlderThan24Hours()
    {
        var job =
            CreateJob(
                ReferenceTime.AddHours(-25));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last24Hours,
                ReferenceTime);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_AcceptsJobInsideThreeMonths()
    {
        var job =
            CreateJob(
                ReferenceTime.AddMonths(-2));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last3Months,
                ReferenceTime);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_RejectsJobOutsideThreeMonths()
    {
        var job =
            CreateJob(
                ReferenceTime.AddMonths(-4));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last3Months,
                ReferenceTime);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_AcceptsJobExactlySixMonthsOld()
    {
        var job =
            CreateJob(
                ReferenceTime.AddMonths(-6));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last6Months,
                ReferenceTime);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_RejectsJobOlderThanSixMonths()
    {
        var job =
            CreateJob(
                ReferenceTime
                    .AddMonths(-6)
                    .AddSeconds(-1));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last6Months,
                ReferenceTime);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_RejectsJobWithoutPublicationDate()
    {
        var job =
            CreateJob(null);

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last6Months,
                ReferenceTime);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_RejectsFuturePublicationDate()
    {
        var job =
            CreateJob(
                ReferenceTime.AddHours(1));

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.IsAllowed(
                job,
                JobPublicationWindow.Last6Months,
                ReferenceTime);

        Assert.False(result);
    }

    [Fact]
    public void Filter_ReturnsOnlyFreshJobs()
    {
        var jobs =
            new[]
            {
                CreateJob(
                    ReferenceTime.AddHours(-2)),

                CreateJob(
                    ReferenceTime.AddDays(-3)),

                CreateJob(
                    ReferenceTime.AddMonths(-7)),

                CreateJob(null)
            };

        var preferences =
            new JobSearchPreferences
            {
                PublicationWindow =
                    JobPublicationWindow.Last7Days
            };

        var filter =
            new JobFreshnessFilter();

        var result =
            filter.Filter(
                jobs,
                preferences,
                ReferenceTime);

        Assert.Equal(
            2,
            result.Count);
    }

    private static JobPosting CreateJob(
        DateTimeOffset? publishedAt)
    {
        return new JobPosting
        {
            Title = "Test Job",
            Company = "Test Company",
            PublishedAt = publishedAt
        };
    }
}