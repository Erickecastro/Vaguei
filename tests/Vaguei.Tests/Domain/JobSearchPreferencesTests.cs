using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class JobSearchPreferencesTests
{
    [Fact]
    public void ShouldIncludeBrazilByDefault()
    {
        var preferences = new JobSearchPreferences();

        Assert.True(preferences.IncludeBrazil);
        Assert.False(preferences.IncludeInternational);
    }

    [Fact]
    public void ShouldAllowMultipleWorkModels()
    {
        var preferences = new JobSearchPreferences
        {
            WorkModels =
            [
                WorkModel.Remote,
                WorkModel.Hybrid
            ]
        };

        Assert.Contains(WorkModel.Remote, preferences.WorkModels);
        Assert.Contains(WorkModel.Hybrid, preferences.WorkModels);
        Assert.DoesNotContain(WorkModel.OnSite, preferences.WorkModels);
    }

    [Fact]
    public void ShouldIgnoreDuplicateDesiredRoles()
    {
        var preferences = new JobSearchPreferences();

        preferences.DesiredRoles.Add("Desenvolvedor .NET");
        preferences.DesiredRoles.Add("desenvolvedor .net");

        Assert.Single(preferences.DesiredRoles);
    }

    [Fact]
    public void ShouldIgnoreDuplicateLocationsWithDifferentCasing()
    {
        var preferences = new JobSearchPreferences();

        preferences.Countries.Add("Brazil");
        preferences.Countries.Add("BRAZIL");

        preferences.States.Add("Amazonas");
        preferences.States.Add("AMAZONAS");

        preferences.Cities.Add("Manaus");
        preferences.Cities.Add("MANAUS");

        Assert.Single(preferences.Countries);
        Assert.Single(preferences.States);
        Assert.Single(preferences.Cities);
    }
}