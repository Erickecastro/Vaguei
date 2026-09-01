namespace Vaguei.Domain.Entities;

public sealed class CandidateProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string ProfessionalTitle { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public HashSet<string> Skills { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<WorkExperience> Experiences { get; set; } = [];
}