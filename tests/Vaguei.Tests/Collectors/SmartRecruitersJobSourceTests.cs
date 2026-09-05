using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Collectors;

public sealed class SmartRecruitersJobSourceTests
{
    [Fact]
    public async Task SearchAsync_MapsBrazilianPublicPosting()
    {
        var handler = new SmartRecruitersHandler();
        var source = new SmartRecruitersJobSource(
            new HttpClient(handler),
            new Dictionary<string, string> { ["Example"] = "Empresa Exemplo" });

        var job = Assert.Single(await source.SearchAsync(new JobSearchQuery
        {
            Keywords = [".NET"],
            Locations = ["Brasil"]
        }));

        Assert.Equal("Pessoa Desenvolvedora .NET", job.Title);
        Assert.Equal("Empresa Exemplo", job.Company);
        Assert.Equal("BR", job.Location.CountryCode);
        Assert.Equal(WorkModel.Hybrid, job.WorkModel);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal("SmartRecruiters", job.Source);
        Assert.Contains("country=br", handler.ListRequestQuery);
        Assert.Equal("https://jobs.smartrecruiters.com/Example/123", job.Url?.ToString());
    }

    [Fact]
    public async Task SearchAsync_UsesCompanySearchWithoutTextQuery()
    {
        var handler = new SmartRecruitersHandler();
        var source = new SmartRecruitersJobSource(
            new HttpClient(handler),
            new Dictionary<string, string> { ["Example"] = "Empresa Exemplo" });

        var jobs = await source.SearchAsync(new JobSearchQuery
        {
            Keywords = ["Empresa Exemplo"]
        });

        Assert.Single(jobs);
        Assert.DoesNotContain("q=", handler.ListRequestQuery);
    }

    private sealed class SmartRecruitersHandler : HttpMessageHandler
    {
        public string ListRequestQuery { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var isList = request.RequestUri!.AbsolutePath.EndsWith(
                "/postings",
                StringComparison.Ordinal);
            if (isList)
            {
                ListRequestQuery = request.RequestUri.Query;
            }

            var json = isList
                ? """{ "content": [{ "id": "123" }] }"""
                : """
                  {
                    "name": "Pessoa Desenvolvedora .NET",
                    "releasedDate": "2026-09-04T12:00:00Z",
                    "location": {
                      "fullLocation": "Campinas, SP, Brazil",
                      "remote": false,
                      "hybrid": true
                    },
                    "typeOfEmployment": { "label": "Full-time" },
                    "department": { "label": "Tecnologia" },
                    "function": { "label": "Engenharia" },
                    "industry": { "label": "Software" },
                    "jobAd": { "sections": {
                      "jobDescription": { "text": "Desenvolvimento de aplicações." },
                      "qualifications": { "text": "C# e .NET obrigatórios." }
                    } },
                    "applyUrl": "https://jobs.smartrecruiters.com/Example/123"
                  }
                  """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
