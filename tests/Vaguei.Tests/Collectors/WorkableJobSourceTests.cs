using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class WorkableJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsBrazilianPublicPosting()
    {
        var source = new WorkableJobSource(
            new HttpClient(new WorkableHandler()),
            new Dictionary<string, string> { ["empresa-exemplo"] = "Empresa Exemplo" });

        var job = Assert.Single(await source.SearchAsync(new JobSearchQuery
        {
            Keywords = [".NET"],
            Locations = ["Brasil"]
        }));

        Assert.Equal("Pessoa Desenvolvedora .NET", job.Title);
        Assert.Equal("Empresa Exemplo", job.Company);
        Assert.Equal("BR", job.Location.CountryCode);
        Assert.Equal(WorkModel.Remote, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal("Workable", job.Source);
        Assert.Equal("empresa-exemplo:ABC123", job.SourcePostingId);
        Assert.Equal("C# e .NET obrigatórios.", job.Description);
        Assert.Equal("https://apply.workable.com/j/ABC123", job.Url?.ToString());
        Assert.Contains("Tecnologia", job.Tags);
    }

    [Fact]
    public async Task SearchAsync_FiltersUnrelatedPostings()
    {
        var source = new WorkableJobSource(
            new HttpClient(new WorkableHandler()),
            new Dictionary<string, string> { ["empresa-exemplo"] = "Empresa Exemplo" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["enfermagem"]
        });

        Assert.Empty(jobs);
    }

    private sealed class WorkableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            const string json = """
                {
                  "jobs": [{
                    "shortcode": "ABC123",
                    "title": "Pessoa Desenvolvedora .NET",
                    "employment_type": "Full-time",
                    "telecommuting": true,
                    "department": "Tecnologia",
                    "url": "https://apply.workable.com/j/ABC123",
                    "published_on": "2026-09-04",
                    "country": "Brazil",
                    "city": "Manaus",
                    "state": "Amazonas",
                    "function": "Engenharia",
                    "industry": "Software",
                    "description": "<p>C# e .NET obrigatórios.</p>"
                  }]
                }
                """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
