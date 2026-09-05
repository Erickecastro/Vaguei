using System.Net;
using System.Text;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Models;
using Vaguei.Domain.Enums;

namespace Vaguei.Tests.Collectors;

public sealed class ArbeitnowJobSourceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsMappedJobs()
    {
        const string json =
            """
            {
              "data": [
                {
                  "slug": "desenvolvedor-dotnet",
                  "company_name": "Empresa Teste",
                  "title": "Desenvolvedor .NET",
                  "description": "Desenvolvimento com C# e ASP.NET Core.",
                  "remote": true,
                  "url": "https://example.com/job",
                  "location": "Brasil",
                  "created_at": 1788264000,
                  "tags": [
                    "C#",
                    ".NET"
                  ]
                }
              ]
            }
            """;

        var httpClient = CreateHttpClient(json);

        var source =
            new ArbeitnowJobSource(httpClient);

        var query =
            new JobSearchQuery();

        var jobs =
            await source.SearchAsync(query);

        var result =
            jobs.ToList();

        Assert.Single(result);

        var job = result[0];

        Assert.Equal(
            "Desenvolvedor .NET",
            job.Title);

        Assert.Equal(
            DateTimeOffset.FromUnixTimeSeconds(
                1788264000),
            job.PublishedAt);

        Assert.Equal(
            "Empresa Teste",
            job.Company);

        Assert.Equal(
            "Brasil",
            job.Location.RawLocation);

        Assert.Equal(
            "Arbeitnow",
            job.Source);

        Assert.Equal(
            "desenvolvedor-dotnet",
            job.SourcePostingId);

        Assert.NotNull(job.Url);

        Assert.Contains("C#", job.Tags);
        Assert.Contains(".NET", job.Tags);
        Assert.Empty(job.Skills);

        Assert.Contains(
            job.SkillRequirements,
            requirement =>
                requirement.Name == ".NET" &&
                requirement.Level == JobSkillRequirementLevel.Core);

        Assert.Contains(
            job.SkillRequirements,
            requirement =>
                requirement.Name == "C#" &&
                requirement.Level == JobSkillRequirementLevel.Mentioned);
    }

    [Fact]
    public async Task SearchAsync_FiltersByKeyword()
    {
        const string json =
            """
            {
              "data": [
                {
                  "company_name": "Empresa A",
                  "title": "Desenvolvedor .NET",
                  "description": "C# e ASP.NET Core",
                  "remote": true,
                  "url": "https://example.com/dotnet",
                  "location": "Brasil"
                },
                {
                  "company_name": "Empresa B",
                  "title": "Desenvolvedor Java",
                  "description": "Spring Boot",
                  "remote": true,
                  "url": "https://example.com/java",
                  "location": "Brasil"
                }
              ]
            }
            """;

        var source =
            new ArbeitnowJobSource(
                CreateHttpClient(json));

        var query =
            new JobSearchQuery
            {
                Keywords =
                [
                    ".NET"
                ]
            };

        var jobs =
            await source.SearchAsync(query);

        var result =
            jobs.ToList();

        Assert.Single(result);

        Assert.Equal(
            "Desenvolvedor .NET",
            result[0].Title);
    }

    [Fact]
    public async Task SearchAsync_FiltersByWorkModel()
    {
        const string json =
            """
            {
              "data": [
                {
                  "company_name": "Empresa A",
                  "title": "Desenvolvedor Remoto",
                  "description": "",
                  "remote": true,
                  "url": "https://example.com/remote",
                  "location": "Brasil"
                },
                {
                  "company_name": "Empresa B",
                  "title": "Desenvolvedor Não Remoto",
                  "description": "",
                  "remote": false,
                  "url": "https://example.com/local",
                  "location": "Manaus"
                }
              ]
            }
            """;

        var source =
            new ArbeitnowJobSource(
                CreateHttpClient(json));

        var query =
            new JobSearchQuery
            {
                WorkModels =
                [
                    WorkModel.Remote
                ]
            };

        var jobs =
            await source.SearchAsync(query);

        var result =
            jobs.ToList();

        Assert.Single(result);

        Assert.Equal(
            "Desenvolvedor Remoto",
            result[0].Title);

        Assert.Equal(
            WorkModel.Remote,
            result[0].WorkModel);
    }

    [Fact]
    public async Task SearchAsync_FiltersByLocation()
    {
        const string json =
            """
            {
              "data": [
                {
                  "company_name": "Empresa A",
                  "title": "Desenvolvedor Backend",
                  "description": "",
                  "remote": false,
                  "url": "https://example.com/manaus",
                  "location": "Manaus, Brasil"
                },
                {
                  "company_name": "Empresa B",
                  "title": "Desenvolvedor Backend",
                  "description": "",
                  "remote": false,
                  "url": "https://example.com/sp",
                  "location": "São Paulo, Brasil"
                }
              ]
            }
            """;

        var source =
            new ArbeitnowJobSource(
                CreateHttpClient(json));

        var query =
            new JobSearchQuery
            {
                Locations =
                [
                    "Manaus"
                ]
            };

        var jobs =
            await source.SearchAsync(query);

        var result =
            jobs.ToList();

        Assert.Single(result);

        Assert.Contains(
            "Manaus",
            result[0].Location.RawLocation ?? string.Empty);
    }

    [Fact]
    public async Task SearchAsync_MapsMissingPublicationDateAsNull()
    {
        const string json =
            """
            {
              "data": [
                {
                  "slug": "test-job",
                  "company_name": "Test Company",
                  "title": ".NET Developer",
                  "description": "C# and ASP.NET Core",
                  "remote": true,
                  "url": "https://example.com/jobs/test",
                  "location": "Remote",
                  "tags": []
                }
              ]
            }
            """;

        using var httpClient =
            new HttpClient(
                new FakeHttpMessageHandler(json));

        var source =
            new ArbeitnowJobSource(
                httpClient);

        var query =
            new JobSearchQuery();

        var jobs =
            await source.SearchAsync(query);

        var job =
            Assert.Single(jobs);

        Assert.Null(
            job.PublishedAt);
    }

    private static HttpClient CreateHttpClient(
        string json)
    {
        var handler =
            new FakeHttpMessageHandler(json);

        return new HttpClient(handler);
    }

    private sealed class FakeHttpMessageHandler(
        string responseContent)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response =
                new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    Content =
                        new StringContent(
                            responseContent,
                            Encoding.UTF8,
                            "application/json")
                };

            return Task.FromResult(response);
        }
    }
}
