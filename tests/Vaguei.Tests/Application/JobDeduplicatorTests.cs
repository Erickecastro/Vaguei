using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobDeduplicatorTests
{
    private readonly JobDeduplicator _deduplicator = new();

    [Fact]
    public void Deduplicate_RemovesEquivalentCrossPostedJobs()
    {
        var first = CreateJob(
            "Software Engineer",
            "Empresa Teste",
            "<p>Desenvolvimento de APIs.</p>",
            "Fonte A");

        var second = CreateJob(
            " software   engineer ",
            "EMPRESA TESTE",
            "Desenvolvimento de APIs.",
            "Fonte B");

        var result = _deduplicator.Deduplicate(
            [first, second]);

        Assert.Single(result);
    }

    [Fact]
    public void Deduplicate_PreservesDifferentOpenings()
    {
        var backend = CreateJob(
            "Software Engineer",
            "Empresa Teste",
            "Atuação em serviços backend.",
            "Fonte A");

        var mobile = CreateJob(
            "Software Engineer",
            "Empresa Teste",
            "Atuação em aplicativos móveis.",
            "Fonte A");

        var result = _deduplicator.Deduplicate(
            [backend, mobile]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Deduplicate_RemovesCrossPostsWithMinorDescriptionDifferences()
    {
        var first = CreateJob(
            "Analista Financeiro",
            "Empresa Teste",
            "Responsável por planejamento financeiro, orçamento, relatórios e análise mensal de resultados.",
            "Fonte A");

        var second = CreateJob(
            "Analista Financeiro",
            "Empresa Teste",
            "Responsável por planejamento financeiro, orçamento, relatórios e análise mensal de resultados. Benefícios competitivos.",
            "Fonte B");

        var result = _deduplicator.Deduplicate([first, second]);

        Assert.Single(result);
    }

    private static JobPosting CreateJob(
        string title,
        string company,
        string description,
        string source)
    {
        return new JobPosting
        {
            Title = title,
            Company = company,
            Description = description,
            Source = source,
            PublishedAt = new DateTimeOffset(
                2026,
                9,
                1,
                0,
                0,
                0,
                TimeSpan.Zero),
            Location = new JobLocation
            {
                RawLocation = "Brasil"
            }
        };
    }
}
