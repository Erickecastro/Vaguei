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

    [Fact]
    public void Deduplicate_UsesStableSourceIdentityWhenContentChanges()
    {
        var first = CreateJob(
            "Analista de Dados",
            "Empresa Teste",
            "Descrição publicada originalmente.",
            "Fonte A");
        first.SourcePostingId = "empresa:123";

        var updated = CreateJob(
            "Analista de Dados Sênior",
            "Empresa Teste",
            "Descrição completamente atualizada pela empresa.",
            "Fonte A");
        updated.SourcePostingId = "empresa:123";
        updated.PublishedAt = first.PublishedAt?.AddHours(1);

        var result = _deduplicator.Deduplicate([first, updated]);

        var selected = Assert.Single(result);
        Assert.Same(updated, selected);
    }

    [Fact]
    public void Deduplicate_PreservesEqualIdentifiersFromDifferentSources()
    {
        var first = CreateJob("Vaga A", "Empresa A", "Descrição A", "Fonte A");
        first.SourcePostingId = "123";
        var second = CreateJob("Vaga B", "Empresa B", "Descrição B", "Fonte B");
        second.SourcePostingId = "123";

        var result = _deduplicator.Deduplicate([first, second]);

        Assert.Equal(2, result.Count);
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
