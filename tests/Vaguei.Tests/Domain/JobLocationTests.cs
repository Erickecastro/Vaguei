using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class JobLocationTests
{
    [Fact]
    public void ShouldStoreStructuredLocation()
    {
        var location = new JobLocation
        {
            Country = "Brazil",
            CountryCode = "BR",
            State = "Amazonas",
            StateCode = "AM",
            City = "Manaus",
            RawLocation = "Manaus, Amazonas, Brazil"
        };

        Assert.Equal("Brazil", location.Country);
        Assert.Equal("BR", location.CountryCode);
        Assert.Equal("Amazonas", location.State);
        Assert.Equal("AM", location.StateCode);
        Assert.Equal("Manaus", location.City);
        Assert.Equal(
            "Manaus, Amazonas, Brazil",
            location.RawLocation);
    }
}