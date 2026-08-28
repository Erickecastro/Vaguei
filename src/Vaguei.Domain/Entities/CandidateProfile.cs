namespace Vaguei.Domain.Entities;

public sealed class CandidateProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public HashSet<string> Skills { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> DesiredRoles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<WorkExperience> Experiences { get; set; } = [];

    public HashSet<string> PreferredLocations { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool AcceptRemoteJobs { get; set; } = true;
}
