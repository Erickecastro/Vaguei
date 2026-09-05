namespace Vaguei.Application.Models;

public sealed record JobSearchSettings
{
    public int SearchScopeIndex { get; init; }
    public int PublicationWindowIndex { get; init; } = 3;
    public int WorkModelIndex { get; init; }
    public int EmploymentTypeIndex { get; init; }
    public int SeniorityIndex { get; init; }
    public string LocationFilter { get; init; } = string.Empty;
}
