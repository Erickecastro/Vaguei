using Vaguei.Domain.Entities;

namespace Vaguei.Application.Interfaces;

public interface IPersistentJobCache
{
    bool TryLoad(
        string source,
        string queryKey,
        out IReadOnlyCollection<JobPosting> jobs,
        out DateTimeOffset expiresAt);

    void Save(
        string source,
        string queryKey,
        DateTimeOffset expiresAt,
        IReadOnlyCollection<JobPosting> jobs);
}
