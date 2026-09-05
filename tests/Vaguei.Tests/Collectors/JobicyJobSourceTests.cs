using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class JobicyJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsAndFiltersPublicJobs()
    {
        const string json =
            """
            {"jobs":[
              {"id":42,"url":"https://jobicy.com/jobs/42","jobTitle":"Data Analyst","companyName":"Acme","jobGeo":"Anywhere","jobExcerpt":"SQL dashboards","jobDescription":"<p>SQL and Power BI</p>","pubDate":"2026-09-05T12:00:00+00:00","jobIndustry":["Data Science"],"jobType":["full-time"]},
              {"id":43,"url":"https://jobicy.com/jobs/43","jobTitle":"Designer","companyName":"Other","jobGeo":"Europe","jobDescription":"Figma","pubDate":"2026-09-05T12:00:00+00:00","jobIndustry":["Design"],"jobType":["contract"]}
            ]}
            """;
        var handler = new CountingHandler(json);
        using var client = new HttpClient(handler);
        var source = new JobicyJobSource(client);

        var jobs = (await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Data Analyst"]
        })).ToArray();

        var job = Assert.Single(jobs);
        Assert.Equal("Jobicy", job.Source);
        Assert.Equal("42", job.SourcePostingId);
        Assert.Equal(WorkModel.Remote, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Contains("SQL and Power BI", job.Description);
        Assert.Equal("https://jobicy.com/jobs/42", job.Url?.AbsoluteUri);
    }

    [Fact]
    public async Task SearchAsync_CachesFeedAcrossDifferentQueries()
    {
        const string json = "{\"jobs\":[]}";
        var handler = new CountingHandler(json);
        using var client = new HttpClient(handler);
        var source = new JobicyJobSource(client);

        await source.SearchAsync(new JobSearchQuery { Keywords = ["developer"] });
        await source.SearchAsync(new JobSearchQuery { Keywords = ["designer"] });

        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class CountingHandler(string json) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
