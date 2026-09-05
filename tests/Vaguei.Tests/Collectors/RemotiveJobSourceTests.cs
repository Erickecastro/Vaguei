using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class RemotiveJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsFiltersAndCachesPublicFeed()
    {
        const string json =
            """
            {"jobs":[
              {"id":10,"url":"https://remotive.com/remote-jobs/software-dev/data-engineer-10","title":"Data Engineer","company_name":"Acme","category":"Software Development","job_type":"full_time","publication_date":"2026-09-05T10:00:00Z","candidate_required_location":"Brazil","description":"<p>Python and SQL</p>"},
              {"id":11,"url":"https://remotive.com/remote-jobs/design/designer-11","title":"Designer","company_name":"Other","category":"Design","job_type":"contract","publication_date":"2026-09-05T09:00:00Z","candidate_required_location":"Worldwide","description":"Figma"}
            ]}
            """;
        var handler = new CountingHandler(json);
        using var client = new HttpClient(handler);
        var source = new RemotiveJobSource(client);

        var jobs = (await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Data Engineer"]
        })).ToArray();
        await source.SearchAsync(new JobSearchQuery { Keywords = ["Designer"] });

        var job = Assert.Single(jobs);
        Assert.Equal("Remotive", job.Source);
        Assert.Equal(WorkModel.Remote, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal("Brasil", job.Location.Country);
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
