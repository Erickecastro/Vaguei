namespace Vaguei.Domain.Entities;

public sealed class WorkExperience
{
    public string Company { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? StartYear { get; set; }

    public int? EndYear { get; set; }

    public bool IsCurrent { get; set; }
}