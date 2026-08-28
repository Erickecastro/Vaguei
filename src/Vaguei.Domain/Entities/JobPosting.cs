using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Entities;

public sealed class JobPosting
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public required string Company { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Location { get; set; }

    public Uri? Url { get; set; }

    public string? Source { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public SeniorityLevel SeniorityLevel { get; set; }

    public WorkModel WorkModel { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset FoundAt { get; init; } = DateTimeOffset.UtcNow;

    public HashSet<string> Skills { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
