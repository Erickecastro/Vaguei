using Vaguei.Application.Interfaces;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobSourceTests
{
    [Fact]
    public async Task JobSource_CanSearchJobs()
    {
        IJobSource source =
            new FakeJobSource();

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
            "Fonte de Teste",
            source.Name);

        Assert.Equal(
            "Desenvolvedor .NET",
            result[0].Title);
    }

    private sealed class FakeJobSource : IJobSource
    {
        public string Name =>
            "Fonte de Teste";

        public Task<IEnumerable<JobPosting>> SearchAsync(
            JobSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<JobPosting> jobs =
            [
                new JobPosting
                {
                    Title = "Desenvolvedor .NET",
                    Company = "Empresa Teste"
                }
            ];

            return Task.FromResult(jobs);
        }
    }
}