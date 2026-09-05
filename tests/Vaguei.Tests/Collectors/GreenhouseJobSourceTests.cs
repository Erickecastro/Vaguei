using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class GreenhouseJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsBrazilianJobFromPublicBoard()
    {
        const string json =
            """
            {
              "jobs": [{
                "title": "Analista Financeiro",
                "content": "<p>Experiência com planejamento financeiro.</p>",
                "absolute_url": "https://boards.greenhouse.io/example/jobs/123",
                "updated_at": "2026-09-03T14:00:00Z",
                "location": { "name": "São Paulo, Brazil" },
                "departments": [{ "name": "Finance" }]
              }]
            }
            """;

        var source = new GreenhouseJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["example"] = "Empresa" });

        var job = Assert.Single(await source.SearchAsync(new JobSearchQuery()));

        Assert.Equal("Analista Financeiro", job.Title);
        Assert.Equal("Empresa", job.Company);
        Assert.Equal("BR", job.Location.CountryCode);
        Assert.Equal("Brasil", job.Location.Country);
        Assert.Equal("Greenhouse", job.Source);
        Assert.Equal(new DateTimeOffset(2026, 9, 3, 14, 0, 0, TimeSpan.Zero), job.PublishedAt);
        Assert.Equal("Experiência com planejamento financeiro.", job.Description);
        Assert.Contains("Finance", job.Tags);
    }

    [Fact]
    public async Task SearchAsync_AppliesResumeKeywords()
    {
        const string json =
            """
            {
              "jobs": [
                { "title": "Analista Financeiro", "content": "Contabilidade", "location": { "name": "Brazil" } },
                { "title": "Pessoa Desenvolvedora", "content": "C# e .NET", "location": { "name": "Brazil" } }
              ]
            }
            """;

        var source = new GreenhouseJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["example"] = "Empresa" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = [".NET"]
        });

        var job = Assert.Single(jobs);
        Assert.Equal("Pessoa Desenvolvedora", job.Title);
    }

    [Fact]
    public async Task SearchAsync_MatchesARelevantTermFromDesiredRole()
    {
        const string json =
            """
            {
              "jobs": [{
                "title": "Senior .NET Engineer",
                "content": "Desenvolvimento de aplicações.",
                "location": { "name": "Brazil" }
              }]
            }
            """;

        var source = new GreenhouseJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["example"] = "Empresa" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Desenvolvedor .NET"]
        });

        Assert.Single(jobs);
    }

    [Fact]
    public async Task SearchAsync_MatchesCompanyName()
    {
        const string json =
            """
            { "jobs": [{
              "title": "Assistente administrativo",
              "content": "Rotinas administrativas.",
              "location": { "name": "Manaus, Brazil" }
            }] }
            """;

        var source = new GreenhouseJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["sidia"] = "Sidia" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Sidia"]
        });

        Assert.Single(jobs);
    }

    [Fact]
    public async Task SearchAsync_DoesNotMatchCompanyInsideAnotherWord()
    {
        const string json =
            """
            { "jobs": [{
              "title": "Especialista de Legal Efficiency",
              "content": "Support the company and its subsidiaries.",
              "location": { "name": "São Paulo, Brazil" }
            }] }
            """;

        var source = new GreenhouseJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["quintoandar"] = "QuintoAndar" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Sidia"]
        });

        Assert.Empty(jobs);
    }

    private static HttpClient CreateHttpClient(string json) =>
        new(new FakeHandler(json));

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
