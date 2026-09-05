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

        var uniqueJobs = new List<JobPosting>();

        foreach (var job in jobs)
        {
            var duplicateIndex = uniqueJobs.FindIndex(existing =>
                AreEquivalent(existing, job));

            if (duplicateIndex < 0)
            {
                uniqueJobs.Add(job);
                continue;
            }

            if (IsPreferred(job, uniqueJobs[duplicateIndex]))
            {
                uniqueJobs[duplicateIndex] = job;
            }
        }

        return uniqueJobs;
    }

    private static bool AreEquivalent(
        JobPosting first,
        JobPosting second)
    {
        if (!Normalize(first.Company).Equals(
                Normalize(second.Company),
                StringComparison.Ordinal) ||
            !Normalize(first.Title).Equals(
                Normalize(second.Title),
                StringComparison.Ordinal))
        {
            return false;
        }

        var firstLocation = Normalize(first.Location.RawLocation);
        var secondLocation = Normalize(second.Location.RawLocation);

        if (firstLocation.Length > 0 &&
            secondLocation.Length > 0 &&
            !firstLocation.Equals(secondLocation, StringComparison.Ordinal))
        {
            return false;
        }

        var firstDescription = Normalize(first.Description);
        var secondDescription = Normalize(second.Description);

        if (firstDescription.Length == 0 || secondDescription.Length == 0)
        {
            return NormalizeUrl(first.Url).Equals(
                NormalizeUrl(second.Url),
                StringComparison.OrdinalIgnoreCase);
        }

        return firstDescription.Equals(secondDescription, StringComparison.Ordinal) ||
               CalculateDescriptionOverlap(
                   firstDescription,
                   secondDescription) >= 0.8;
    }

    private static bool IsPreferred(JobPosting candidate, JobPosting existing)
    {
        if (candidate.PublishedAt != existing.PublishedAt)
        {
            return candidate.PublishedAt.GetValueOrDefault(DateTimeOffset.MinValue) >
                   existing.PublishedAt.GetValueOrDefault(DateTimeOffset.MinValue);
        }

        return candidate.Description.Length > existing.Description.Length;
    }

    private static double CalculateDescriptionOverlap(string first, string second)
    {
        var firstTerms = ToTerms(first);
        var secondTerms = ToTerms(second);

        if (firstTerms.Count == 0 || secondTerms.Count == 0)
        {
            return 0;
        }

        var intersectionCount = firstTerms.Intersect(secondTerms).Count();

        return (double)intersectionCount /
               Math.Min(firstTerms.Count, secondTerms.Count);
    }

    private static HashSet<string> ToTerms(string value)
    {
        return Regex.Matches(value, @"[\p{L}\p{N}+#.]+")
            .Select(match => match.Value)
            .Where(term => term.Length >= 3)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(Uri? url)
    {
        return url is null
            ? string.Empty
            : $"{url.Host}{url.AbsolutePath}".TrimEnd('/');
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
