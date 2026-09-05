using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class AshbyJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsListedBrazilianJob()
    {
        const string json =
            """
            { "jobs": [{
              "title": "Executivo de vendas",
              "descriptionPlain": "Prospecção e relacionamento.",
              "location": "Brazil - São Paulo",
              "publishedAt": "2026-09-01T12:00:00Z",
              "isListed": true,
              "isRemote": false,
              "workplaceType": "Hybrid",
              "employmentType": "FullTime",
              "department": "Sales",
              "team": "LATAM",
              "jobUrl": "https://jobs.ashbyhq.com/example/123"
            }] }
            """;

        var source = new AshbyJobSource(
            new HttpClient(new FakeHandler(json)),
            new Dictionary<string, string> { ["example"] = "Empresa" });

        var job = Assert.Single(await source.SearchAsync(new JobSearchQuery()));

        Assert.Equal("BR", job.Location.CountryCode);
        Assert.Equal(WorkModel.Hybrid, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal("Ashby", job.Source);
        Assert.Contains("Sales", job.Tags);
    }

    private sealed class FakeHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
