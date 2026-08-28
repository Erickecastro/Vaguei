namespace Vaguei.Domain.Entities;

public sealed class WorkExperience
{
    public string Company { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
