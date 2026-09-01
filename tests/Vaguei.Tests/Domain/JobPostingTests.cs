using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class JobPostingTests
{
    [Fact]
    public void NewJobPosting_HasLocationObject()
    {
        var job = new JobPosting
        {
            Title = "Desenvolvedor .NET",
            Company = "Empresa Teste"
        };

        Assert.NotNull(job.Location);
    }

    [Fact]
    public void JobPosting_AllowsStructuredLocation()
    {
        var job = new JobPosting
        {
            Title = "Desenvolvedor .NET",
            Company = "Empresa Teste",

            Location = new JobLocation
            {
                Country = "Brazil",
                CountryCode = "BR",
                State = "Amazonas",
                StateCode = "AM",
                City = "Manaus",
                RawLocation = "Manaus, Amazonas, Brazil"
            }
        };

        Assert.Equal(
            "Brazil",
            job.Location.Country);

        Assert.Equal(
            "BR",
            job.Location.CountryCode);

        Assert.Equal(
            "Amazonas",
            job.Location.State);

        Assert.Equal(
            "AM",
            job.Location.StateCode);

        Assert.Equal(
            "Manaus",
            job.Location.City);

        Assert.Equal(
            "Manaus, Amazonas, Brazil",
            job.Location.RawLocation);
    }
}