using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class JoobleJobSourceTests
{
    [Fact]
    public async Task SearchAsync_SendsQueryAndMapsResults()
    {
        const string json =
            """
            {"totalCount":1,"jobs":[{"id":42,"title":"Analista de Dados","location":"Manaus, AM","snippet":"<p>SQL e Power BI, trabalho híbrido.</p>","type":"Full-time","link":"https://example.com/jobs/42","company":"Empresa Exemplo","updated":"2026-09-05T12:00:00Z"}]}
            """;
        var handler = new RecordingHandler(json);
        using var client = new HttpClient(handler);
        var source = new JoobleJobSource(client, "secret key");

        var jobs = (await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Analista de Dados"],
            Locations = ["Manaus"]
        })).ToArray();

        var job = Assert.Single(jobs);
        Assert.Equal("Jooble", job.Source);
        Assert.Equal("42", job.SourcePostingId);
        Assert.Equal("Empresa Exemplo", job.Company);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal(WorkModel.Hybrid, job.WorkModel);
        Assert.Equal("Brasil", job.Location.Country);
        Assert.Equal("https://example.com/jobs/42", job.Url?.AbsoluteUri);
        Assert.Equal("https://jooble.org/api/secret%20key", handler.RequestUri?.AbsoluteUri);
        Assert.Contains("\"keywords\":\"Analista de Dados\"", handler.RequestBody);
        Assert.Contains("\"location\":\"Manaus\"", handler.RequestBody);
    }

    [Fact]
    public void Constructor_RejectsMissingCredential()
    {
        using var client = new HttpClient();

        Assert.Throws<ArgumentException>(() => new JoobleJobSource(client, " "));
    }

    private sealed class RecordingHandler(string json) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
