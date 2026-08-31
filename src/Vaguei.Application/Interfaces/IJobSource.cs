using Vaguei.Domain.Models;
using Vaguei.Domain.Entities;

namespace Vaguei.Application.Interfaces;

public interface IJobSource
{
    string Name { get; }

    Task<IEnumerable<JobPosting>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default);
}