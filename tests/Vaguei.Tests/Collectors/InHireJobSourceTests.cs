using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class InHireJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsPublishedJobFromPublicCareerPage()
    {
        var handler = new InHireHandler();
        var source = new InHireJobSource(
            new HttpClient(handler),
            new Dictionary<string, string> { ["sidia"] = "Sidia" });

        var job = Assert.Single(await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Sidia"]
        }));

        Assert.Equal("Desenvolvedor de Software JR", job.Title);
        Assert.Equal("Sidia", job.Company);
        Assert.Equal("BR", job.Location.CountryCode);
        Assert.Equal(WorkModel.OnSite, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal("InHire", job.Source);
        Assert.Equal(
            new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero),
            job.PublishedAt);
        Assert.Equal("https://sidia.inhire.app/sidia/vagas/job-1", job.Url?.ToString());
        Assert.All(handler.Requests, request => Assert.Equal("sidia", request.Tenant));
    }

    [Fact]
    public async Task SearchAsync_FiltersUnrelatedTerms()
    {
        var source = new InHireJobSource(
            new HttpClient(new InHireHandler()),
            new Dictionary<string, string> { ["sidia"] = "Sidia" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["odontologia"]
        });

        Assert.Empty(jobs);
    }

    private sealed class InHireHandler : HttpMessageHandler
    {
        public List<(string Path, string? Tenant)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var tenant = request.Headers.TryGetValues("X-Tenant", out var values)
                ? values.Single()
                : null;
            Requests.Add((path, tenant));

            var json = path.EndsWith("/lean", StringComparison.Ordinal)
                ? """
                  [{
                    "jobId": "job-1",
                    "displayName": "Desenvolvedor de Software JR",
                    "link": "https://sidia.inhire.app/sidia/vagas/job-1"
                  }]
                  """
                : """
                  {
                    "displayName": "Desenvolvedor de Software JR",
                    "description": "Desenvolvimento de aplicações C# e .NET.",
                    "location": "Manaus, AM, BR",
                    "workplaceType": "On-site",
                    "contractType": ["full time"],
                    "status": "published",
                    "publishedAt": "2026-09-02T12:00:00Z",
                    "lastPublishedAt": "2026-09-03T12:00:00Z"
                  }
                  """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
