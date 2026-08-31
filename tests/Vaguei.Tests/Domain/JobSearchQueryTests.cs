using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class JobSearchQueryTests
{
    [Fact]
    public void NewQuery_HasEmptyCollections()
    {
        var query = new JobSearchQuery();

        Assert.Empty(query.Keywords);
        Assert.Empty(query.Locations);
        Assert.Empty(query.EmploymentTypes);
        Assert.Empty(query.SeniorityLevels);
    }

    [Fact]
    public void NewQuery_IncludesRemoteByDefault()
    {
        var query = new JobSearchQuery();

        Assert.True(query.IncludeRemote);
    }

    [Fact]
    public void Query_AllowsKeywords()
    {
        var query = new JobSearchQuery
        {
            Keywords =
            [
                ".NET",
                "Backend",
                "ASP.NET Core"
            ]
        };

        Assert.Equal(3, query.Keywords.Count);
        Assert.Contains(".NET", query.Keywords);
        Assert.Contains("Backend", query.Keywords);
        Assert.Contains("ASP.NET Core", query.Keywords);
    }

    [Fact]
    public void Query_AllowsLocations()
    {
        var query = new JobSearchQuery
        {
            Locations =
            [
                "Brasil",
                "Manaus"
            ]
        };

        Assert.Equal(2, query.Locations.Count);
        Assert.Contains("Brasil", query.Locations);
        Assert.Contains("Manaus", query.Locations);
    }

    [Fact]
    public void Query_AllowsRemoteToBeDisabled()
    {
        var query = new JobSearchQuery
        {
            IncludeRemote = false
        };

        Assert.False(query.IncludeRemote);
    }
}