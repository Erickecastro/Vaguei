using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
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

        if (!MatchesExplicitLocations(job.Location, preferences))
        {
            return false;
        }

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

    private static bool MatchesExplicitLocations(
        JobLocation location,
        JobSearchPreferences preferences)
    {
        var requested = preferences.Cities
            .Concat(preferences.States)
            .Concat(preferences.Countries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (requested.Length == 0)
        {
            return true;
        }

        var searchable = Normalize(string.Join(' ',
            location.RawLocation,
            location.City,
            location.State,
            location.StateCode,
            location.Country,
            location.CountryCode));

        return requested.Any(term => ContainsLocationTerm(searchable, term));
    }

    private static bool ContainsLocationTerm(string searchable, string term)
    {
        if (term.Length > 2)
        {
            return searchable.Contains(term, StringComparison.Ordinal);
        }

        return Regex.IsMatch(
            searchable,
            $@"(?:^|[^a-z0-9]){Regex.Escape(term)}(?:$|[^a-z0-9])",
            RegexOptions.CultureInvariant);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
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
               (BrazilLocationPattern().IsMatch(location.RawLocation) ||
                BrazilianCityPattern().IsMatch(location.RawLocation) ||
                BrazilStateCodePattern().IsMatch(location.RawLocation));
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

    [GeneratedRegex(
        @"(?:^|[\s,;/()\-])(s[aã]o paulo|rio de janeiro|belo horizonte|curitiba|porto alegre|bras[ií]lia|recife|salvador|campinas|florian[oó]polis|manaus|fortaleza|goi[aâ]nia|vit[oó]ria|barueri|osasco|guarulhos|sorocaba|santos|ribeir[aã]o preto|s[aã]o jos[eé] dos campos|niter[oó]i|joinville|blumenau|londrina|maring[aá]|uber[aâ]ndia|contagem|caxias do sul)(?:$|[\s,;/()\-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BrazilianCityPattern();

    [GeneratedRegex(
        @"(?:^|[\s,;/()\-])(AC|AL|AP|AM|BA|CE|DF|ES|GO|MA|MT|MS|MG|PA|PB|PR|PE|PI|RJ|RN|RS|RO|RR|SC|SP|SE|TO)(?:$|[\s,;/()\-])",
        RegexOptions.CultureInvariant)]
    private static partial Regex BrazilStateCodePattern();
}
