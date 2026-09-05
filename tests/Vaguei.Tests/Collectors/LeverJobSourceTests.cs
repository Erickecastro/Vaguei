using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class LeverJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsBrazilianJobFromPublicBoard()
    {
        const string json =
            """
            [{
              "text": "Assistente de Relacionamento",
              "descriptionPlain": "Atendimento aos clientes.",
              "additionalPlain": "Benefícios.",
              "hostedUrl": "https://jobs.lever.co/example/123",
              "createdAt": 1788444000000,
              "workplaceType": "on-site",
              "categories": {
                "location": "São Paulo, Brazil",
                "commitment": "Full-time",
                "team": "Operations",
                "department": "Customer Experience"
              },
              "lists": [{ "content": "<li>Boa comunicação</li>" }]
            }]
            """;

        var source = new LeverJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["example"] = "Empresa" });

        var job = Assert.Single(await source.SearchAsync(new JobSearchQuery()));

        Assert.Equal("Assistente de Relacionamento", job.Title);
        Assert.Equal("Empresa", job.Company);
        Assert.Equal("BR", job.Location.CountryCode);
        Assert.Equal(WorkModel.OnSite, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal("Lever", job.Source);
        Assert.Contains("Boa comunicação", job.Description);
        Assert.Contains("Operations", job.Tags);
    }

    [Fact]
    public async Task SearchAsync_KeepsUnknownWorkModelForCentralMatching()
    {
        const string json =
            """
            [{
              "text": "Designer",
              "descriptionPlain": "Design de produto",
                "categories": { "location": "São Paulo" }
            }]
            """;

        var source = new LeverJobSource(
            CreateHttpClient(json),
            new Dictionary<string, string> { ["example"] = "Empresa" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            WorkModels = [WorkModel.Remote]
        });

        var job = Assert.Single(jobs);
        Assert.Equal("BR", job.Location.CountryCode);
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
