using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobFreshnessFilter
{
    public IReadOnlyCollection<JobPosting> Filter(
        IEnumerable<JobPosting> jobs,
        JobSearchPreferences preferences,
        DateTimeOffset referenceTime)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(preferences);

        return jobs
            .Where(
                job =>
                    IsAllowed(
                        job,
                        preferences.PublicationWindow,
                        referenceTime))
            .ToList();
    }

    public bool IsAllowed(
        JobPosting job,
        JobPublicationWindow publicationWindow,
        DateTimeOffset referenceTime)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.PublishedAt is null)
        {
            return false;
        }

        var publishedAt =
            job.PublishedAt.Value;

        if (publishedAt > referenceTime)
        {
            return false;
        }

        var maximumAllowedDate =
            referenceTime.AddMonths(-6);

        if (publishedAt < maximumAllowedDate)
        {
            return false;
        }

        var requestedMinimumDate =
            GetMinimumDate(
                publicationWindow,
                referenceTime);

        return publishedAt >= requestedMinimumDate;
    }

    private static DateTimeOffset GetMinimumDate(
        JobPublicationWindow publicationWindow,
        DateTimeOffset referenceTime)
    {
        return publicationWindow switch
        {
            JobPublicationWindow.Last24Hours =>
                referenceTime.AddHours(-24),

            JobPublicationWindow.Last3Days =>
                referenceTime.AddDays(-3),

            JobPublicationWindow.Last7Days =>
                referenceTime.AddDays(-7),

            JobPublicationWindow.Last30Days =>
                referenceTime.AddDays(-30),

            JobPublicationWindow.Last3Months =>
                referenceTime.AddMonths(-3),

            JobPublicationWindow.Last6Months =>
                referenceTime.AddMonths(-6),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(publicationWindow),
                    publicationWindow,
                    "Janela de publicação inválida.")
        };
    }
}