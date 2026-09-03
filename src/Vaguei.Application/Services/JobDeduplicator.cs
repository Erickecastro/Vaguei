using System.Net;
using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;

namespace Vaguei.Application.Services;

public sealed class JobDeduplicator
{
    public IReadOnlyCollection<JobPosting> Deduplicate(
        IEnumerable<JobPosting> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        return jobs
            .GroupBy(
                CreateKey,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
                group
                    .OrderByDescending(job => job.PublishedAt)
                    .ThenBy(job => job.Source)
                    .First())
            .ToArray();
    }

    private static string CreateKey(
        JobPosting job)
    {
        var company = Normalize(job.Company);
        var title = Normalize(job.Title);
        var description = Normalize(job.Description);

        if (!string.IsNullOrWhiteSpace(description))
        {
            return $"{company}|{title}|{description}";
        }

        var location = Normalize(
            job.Location.RawLocation);

        var url = job.Url?.AbsoluteUri ?? string.Empty;

        return $"{company}|{title}|{location}|{url}";
    }

    private static string Normalize(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(value);
        var withoutMarkup = Regex.Replace(
            decoded,
            "<[^>]+>",
            " ");

        return Regex.Replace(
                withoutMarkup,
                @"\s+",
                " ")
            .Trim()
            .ToLowerInvariant();
    }
}
