using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class JobSkillRequirement
{
    public JobSkillRequirement(
        string name,
        JobSkillRequirementLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "O nome da competência exigida não pode ser vazio.",
                nameof(name));
        }

        Name = name.Trim();
        Level = level;
    }

    public string Name { get; }

    public JobSkillRequirementLevel Level { get; }
}
