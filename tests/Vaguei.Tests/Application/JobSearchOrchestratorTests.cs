using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobSearchOrchestratorTests
{
    private static readonly DateTimeOffset ReferenceTime =
        new(
            2026,
            9,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task SearchAsync_AggregatesRanksAndDeduplicatesSources()
    {
        var matchingJob = CreateJob(
            "Analista de Dados",
            "SQL",
            "Fonte A");

        var duplicate = CreateJob(
            "ANALISTA DE DADOS",
            "SQL",
            "Fonte B");

        var unrelatedJob = CreateJob(
            "Assistente Administrativo",
            "Rotinas administrativas",
            "Fonte B");

        var orchestrator = new JobSearchOrchestrator(
        [
            new StubJobSource("Fonte A", [matchingJob]),
            new StubJobSource("Fonte B", [duplicate, unrelatedJob])
        ]);

        var profile = new CandidateProfile
        {
            ProfessionalTitle = "Analista de Dados",
            Skills = ["SQL"]
        };

        var result = await orchestrator.SearchAsync(
            profile,
            new JobSearchPreferences(),
            ReferenceTime);

        Assert.Equal(3, result.CollectedJobCount);
        Assert.Equal(2, result.UniqueJobCount);
        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(
            "Analista de Dados",
            result.Matches.First().Job.Title);
        Assert.Empty(result.SourceFailures);
    }

    [Fact]
    public async Task SearchAsync_IsolatesSourceFailure()
    {
        var orchestrator = new JobSearchOrchestrator(
        [
            new FailingJobSource("Fonte com erro"),
            new StubJobSource(
                "Fonte saudável",
                [
                    CreateJob(
                        "Analista",
                        "Excel",
                        "Fonte saudável")
                ])
        ]);

        var result = await orchestrator.SearchAsync(
            new CandidateProfile
            {
                ProfessionalTitle = "Analista"
            },
            new JobSearchPreferences(),
            ReferenceTime);

        Assert.Single(result.Matches);

        var failure = Assert.Single(
            result.SourceFailures);

        Assert.Equal(
            "Fonte com erro",
            failure.Source);
    }

    [Fact]
    public async Task SearchAsync_AppliesFreshnessCentrally()
    {
        var oldJob = CreateJob(
            "Analista",
            "Excel",
            "Fonte");

        oldJob.PublishedAt = ReferenceTime.AddMonths(-7);

        var orchestrator = new JobSearchOrchestrator(
        [
            new StubJobSource("Fonte", [oldJob])
        ]);

        var result = await orchestrator.SearchAsync(
            new CandidateProfile
            {
                ProfessionalTitle = "Analista"
            },
            new JobSearchPreferences(),
            ReferenceTime);

        Assert.Equal(1, result.CollectedJobCount);
        Assert.Equal(0, result.UniqueJobCount);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task SearchAsync_BrazilOnly_RejectsInternationalJobsCentrally()
    {
        var brazilianJob = CreateJob(
            "Analista de Dados",
            "SQL",
            "Fonte");

        var internationalJob = CreateJob(
            "Data Analyst",
            "SQL",
            "Fonte");

        internationalJob.Location.RawLocation =
            "London, United Kingdom";

        var orchestrator = new JobSearchOrchestrator(
        [
            new StubJobSource(
                "Fonte",
                [brazilianJob, internationalJob])
        ]);

        var result = await orchestrator.SearchAsync(
            new CandidateProfile
            {
                ProfessionalTitle = "Analista de Dados",
                Skills = ["SQL"]
            },
            new JobSearchPreferences(),
            ReferenceTime);

        var match = Assert.Single(result.Matches);

        Assert.Equal(
            "Brasil",
            match.Job.Location.RawLocation);
    }

    private static JobPosting CreateJob(
        string title,
        string description,
        string source)
    {
        return new JobPosting
        {
            Title = title,
            Company = "Empresa Teste",
            Description = description,
            Source = source,
            PublishedAt = ReferenceTime.AddDays(-1),
            Location = new JobLocation
            {
                RawLocation = "Brasil"
            }
        };
    }

    private sealed class StubJobSource(
        string name,
        IReadOnlyCollection<JobPosting> jobs)
        : IJobSource
    {
        public string Name => name;

        public Task<IEnumerable<JobPosting>> SearchAsync(
            JobSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<JobPosting>>(jobs);
        }
    }

    private sealed class FailingJobSource(
        string name)
        : IJobSource
    {
        public string Name => name;

        public Task<IEnumerable<JobPosting>> SearchAsync(
            JobSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            throw new HttpRequestException(
                "Falha simulada.");
        }
    }
}
