using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed partial class JobGeographyFilter
{
    public IReadOnlyCollection<JobPosting> Filter(
        IEnumerable<JobPosting> jobs,
        JobSearchPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(preferences);

        return jobs
            .Where(job => IsAllowed(job, preferences))
            .ToArray();
    }

    public bool IsAllowed(
        JobPosting job,
        JobSearchPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(preferences);

        if (preferences.IncludeBrazil &&
            preferences.IncludeInternational)
        {
            return true;
        }

        var isBrazilian = IsBrazilian(job.Location);

        if (preferences.IncludeBrazil)
        {
            return isBrazilian;
        }

        return preferences.IncludeInternational &&
               !isBrazilian;
    }

    private static bool IsBrazilian(
        JobLocation location)
    {
        if (!string.IsNullOrWhiteSpace(location.CountryCode))
        {
            return location.CountryCode.Equals(
                "BR",
                StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(location.Country))
        {
            return IsBrazilName(location.Country);
        }

        return !string.IsNullOrWhiteSpace(location.RawLocation) &&
               BrazilLocationPattern().IsMatch(location.RawLocation);
    }

    private static bool IsBrazilName(
        string country)
    {
        return country.Trim().Equals(
                   "Brasil",
                   StringComparison.OrdinalIgnoreCase) ||
               country.Trim().Equals(
                   "Brazil",
                   StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(
        @"(?:^|[\s,;/()\-])(brasil|brazil)(?:$|[\s,;/()\-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BrazilLocationPattern();
}
