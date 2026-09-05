using Vaguei.Domain.Models;

namespace Vaguei.Application.Models;

public sealed class JobSearchExecutionResult
{
    public required JobSearchQuery Query { get; init; }

    public required IReadOnlyCollection<JobMatchResult> Matches { get; init; }

    public required IReadOnlyCollection<JobSourceFailure> SourceFailures { get; init; }

    public required IReadOnlyCollection<JobSourceSearchSummary> SourceSummaries { get; init; }

    public int CollectedJobCount { get; init; }

    public int UniqueJobCount { get; init; }

    public bool AllSourcesFailed { get; init; }

    public int SourcesWithResults => SourceSummaries.Count(summary => summary.JobCount > 0);
}
