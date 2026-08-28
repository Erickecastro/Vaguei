using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class SkillDefinition
{
    public required string Name { get; init; }

    public required SkillCategory Category { get; init; }

    public IReadOnlyCollection<string> Aliases { get; init; } = [];
}
