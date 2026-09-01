using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class JobSearchPreferences
{
    public HashSet<string> DesiredRoles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<WorkModel> WorkModels { get; set; } = [];

    public HashSet<EmploymentType> EmploymentTypes { get; set; } = [];

    public HashSet<SeniorityLevel> SeniorityLevels { get; set; } = [];

    public HashSet<string> Countries { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> States { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Cities { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IncludeBrazil { get; set; } = true;

    public bool IncludeInternational { get; set; }

    public JobPublicationWindow PublicationWindow { get; set; } =
        JobPublicationWindow.Last6Months;
}