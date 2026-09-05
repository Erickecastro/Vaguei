using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobGeographyFilterTests
{
    [Theory]
    [InlineData("Brasil")]
    [InlineData("Brazil")]
    [InlineData("Remoto - Brasil")]
    [InlineData("São Paulo, Brazil")]
    public void IsAllowed_BrazilOnly_AcceptsBrazilianLocations(
        string rawLocation)
    {
        var result = new JobGeographyFilter().IsAllowed(
            CreateJob(rawLocation),
            new JobSearchPreferences());

        Assert.True(result);
    }

    [Theory]
    [InlineData("Berlin, Germany")]
    [InlineData("London, United Kingdom")]
    [InlineData("Remote")]
    [InlineData(null)]
    public void IsAllowed_BrazilOnly_RejectsInternationalOrUnknownLocations(
        string? rawLocation)
    {
        var result = new JobGeographyFilter().IsAllowed(
            CreateJob(rawLocation),
            new JobSearchPreferences());

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_BrazilAndInternational_AcceptsEveryLocation()
    {
        var preferences = new JobSearchPreferences
        {
            IncludeBrazil = true,
            IncludeInternational = true
        };

        var filter = new JobGeographyFilter();

        Assert.True(filter.IsAllowed(
            CreateJob("São Paulo, Brasil"),
            preferences));
        Assert.True(filter.IsAllowed(
            CreateJob("Berlin, Germany"),
            preferences));
        Assert.True(filter.IsAllowed(
            CreateJob(null),
            preferences));
    }

    [Fact]
    public void IsAllowed_PrefersStructuredCountryCode()
    {
        var job = CreateJob("São Paulo, Brasil");
        job.Location.CountryCode = "US";

        var result = new JobGeographyFilter().IsAllowed(
            job,
            new JobSearchPreferences());

        Assert.False(result);
    }

    private static JobPosting CreateJob(
        string? rawLocation)
    {
        return new JobPosting
        {
            Title = "Vaga de teste",
            Company = "Empresa",
            Location = new JobLocation
            {
                RawLocation = rawLocation
            }
        };
    }
}
