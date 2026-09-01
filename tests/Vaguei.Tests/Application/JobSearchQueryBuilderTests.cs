using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobSearchQueryBuilderTests
{
    [Fact]
    public void Build_UsesProfessionalTitleWhenNoDesiredRolesExist()
    {
        var profile = new CandidateProfile
        {
            ProfessionalTitle = "Desenvolvedor .NET"
        };

        var preferences =
            new JobSearchPreferences();

        var builder =
            new JobSearchQueryBuilder();

        var query =
            builder.Build(
                profile,
                preferences);

        Assert.Single(query.Keywords);

        Assert.Contains(
            "Desenvolvedor .NET",
            query.Keywords);
    }

    [Fact]
    public void Build_UsesDesiredRolesWhenTheyExist()
    {
        var profile = new CandidateProfile
        {
            ProfessionalTitle = "Desenvolvedor .NET"
        };

        var preferences =
            new JobSearchPreferences();

        preferences.DesiredRoles.Add(
            "Backend Developer");

        preferences.DesiredRoles.Add(
            ".NET Developer");

        var builder =
            new JobSearchQueryBuilder();

        var query =
            builder.Build(
                profile,
                preferences);

        Assert.Equal(
            2,
            query.Keywords.Count);

        Assert.Contains(
            "Backend Developer",
            query.Keywords);

        Assert.Contains(
            ".NET Developer",
            query.Keywords);

        Assert.DoesNotContain(
            "Desenvolvedor .NET",
            query.Keywords);
    }

    [Fact]
    public void Build_CopiesSearchFilters()
    {
        var profile =
            new CandidateProfile();

        var preferences =
            new JobSearchPreferences();

        preferences.WorkModels.Add(
            WorkModel.Remote);

        preferences.WorkModels.Add(
            WorkModel.Hybrid);

        preferences.EmploymentTypes.Add(
            EmploymentType.FullTime);

        preferences.SeniorityLevels.Add(
            SeniorityLevel.Junior);

        var builder =
            new JobSearchQueryBuilder();

        var query =
            builder.Build(
                profile,
                preferences);

        Assert.Contains(
            WorkModel.Remote,
            query.WorkModels);

        Assert.Contains(
            WorkModel.Hybrid,
            query.WorkModels);

        Assert.Contains(
            EmploymentType.FullTime,
            query.EmploymentTypes);

        Assert.Contains(
            SeniorityLevel.Junior,
            query.SeniorityLevels);
    }

    [Fact]
    public void Build_CombinesLocationPreferences()
    {
        var profile =
            new CandidateProfile();

        var preferences =
            new JobSearchPreferences();

        preferences.Countries.Add(
            "Brazil");

        preferences.States.Add(
            "Amazonas");

        preferences.Cities.Add(
            "Manaus");

        var builder =
            new JobSearchQueryBuilder();

        var query =
            builder.Build(
                profile,
                preferences);

        Assert.Equal(
            3,
            query.Locations.Count);

        Assert.Contains(
            "Brazil",
            query.Locations);

        Assert.Contains(
            "Amazonas",
            query.Locations);

        Assert.Contains(
            "Manaus",
            query.Locations);
    }

    [Fact]
    public void Build_IgnoresEmptyValuesAndDuplicates()
    {
        var profile = new CandidateProfile
        {
            ProfessionalTitle = "  Desenvolvedor .NET  "
        };

        var preferences =
            new JobSearchPreferences();

        preferences.Countries.Add(
            "Brazil");

        preferences.States.Add(
            "brazil");

        preferences.Cities.Add(
            " ");

        var builder =
            new JobSearchQueryBuilder();

        var query =
            builder.Build(
                profile,
                preferences);

        Assert.Single(query.Keywords);

        Assert.Equal(
            "Desenvolvedor .NET",
            query.Keywords[0]);

        Assert.Single(query.Locations);

        Assert.Equal(
            "Brazil",
            query.Locations[0]);
    }
}