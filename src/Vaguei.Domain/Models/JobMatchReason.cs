using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class JobMatchReason
{
    public required JobMatchCriterion Criterion { get; init; }

    public required JobMatchReasonKind Kind { get; init; }

    public required string Description { get; init; }
}