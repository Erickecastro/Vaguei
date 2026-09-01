using Vaguei.Domain.Entities;

namespace Vaguei.Domain.Models;

public sealed class JobMatchResult
{
    public JobMatchResult(
        JobPosting job,
        double score,
        IEnumerable<JobMatchReason> reasons)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(reasons);

        if (score is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                score,
                "A pontuação deve estar entre 0 e 100.");
        }

        Job = job;
        Score = score;
        Reasons = reasons.ToArray();
    }

    public JobPosting Job { get; }

    public double Score { get; }

    public IReadOnlyCollection<JobMatchReason> Reasons { get; }
}