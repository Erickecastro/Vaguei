using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class CandidateSkill
{
    public CandidateSkill(
        string name,
        SkillRelevance relevance)
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
    }

    public string Name { get; }

    public SkillRelevance Relevance { get; }
}