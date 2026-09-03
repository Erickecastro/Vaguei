using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class CandidateSkill
{
    public CandidateSkill(
        string name,
        SkillRelevance relevance,
        IEnumerable<SkillEvidence>? evidence = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "O nome da competência não pode ser vazio.",
                nameof(name));
        }

        Name =
            name.Trim();

        Relevance =
            relevance;

        Evidence = evidence?
            .Distinct()
            .ToArray() ?? [];
    }

    public string Name { get; }

    public SkillRelevance Relevance { get; }

    public IReadOnlyCollection<SkillEvidence> Evidence { get; }
}
