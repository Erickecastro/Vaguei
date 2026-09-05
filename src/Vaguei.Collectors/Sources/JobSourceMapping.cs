using System.Net;
using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Collectors.Sources;

internal static partial class JobSourceMapping
{
    public static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutMarkup = MarkupPattern().Replace(value, " ");

        return WhiteSpacePattern()
            .Replace(WebUtility.HtmlDecode(withoutMarkup), " ")
            .Trim();
    }

    public static JobLocation MapLocation(string? rawLocation)
    {
        var normalized = PlainText(rawLocation);
        var isBrazil = BrazilPattern().IsMatch(normalized);

        return new JobLocation
        {
            RawLocation = normalized,
            Country = isBrazil ? "Brasil" : null,
            CountryCode = isBrazil ? "BR" : null
        };
    }

    public static WorkModel MapWorkModel(
        string? location,
        string? workplaceType = null)
    {
        var value = $"{location} {workplaceType}";

        if (HybridPattern().IsMatch(value))
        {
            return WorkModel.Hybrid;
        }

        if (RemotePattern().IsMatch(value))
        {
            return WorkModel.Remote;
        }

        if (OnSitePattern().IsMatch(value))
        {
            return WorkModel.OnSite;
        }

        return WorkModel.Unknown;
    }

    public static EmploymentType MapEmploymentType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmploymentType.Unknown;
        }

        if (InternshipPattern().IsMatch(value))
        {
            return EmploymentType.Internship;
        }

        if (PartTimePattern().IsMatch(value))
        {
            return EmploymentType.PartTime;
        }

        if (ContractPattern().IsMatch(value))
        {
            return EmploymentType.Contract;
        }

        if (TemporaryPattern().IsMatch(value))
        {
            return EmploymentType.Temporary;
        }

        if (FreelancePattern().IsMatch(value))
        {
            return EmploymentType.Freelance;
        }

        if (FullTimePattern().IsMatch(value))
        {
            return EmploymentType.FullTime;
        }

        return EmploymentType.Unknown;
    }

    public static bool MatchesQuery(JobPosting job, JobSearchQuery query)
    {
        if (query.Keywords.Count > 0 &&
            !query.Keywords.Any(keyword =>
                ContainsSearchTerm(
                    job.Title,
                    job.Company,
                    job.Description,
                    keyword)))
        {
            return false;
        }

        if (query.WorkModels.Count > 0 &&
            job.WorkModel != WorkModel.Unknown &&
            !query.WorkModels.Contains(job.WorkModel))
        {
            return false;
        }

        if (query.EmploymentTypes.Count > 0 &&
            job.EmploymentType != EmploymentType.Unknown &&
            !query.EmploymentTypes.Contains(job.EmploymentType))
        {
            return false;
        }

        return true;
    }

    private static bool ContainsSearchTerm(
        string title,
        string company,
        string description,
        string keyword)
    {
        var searchableText = $"{title}\n{company}\n{description}";

        if (ContainsWholePhrase(searchableText, keyword))
        {
            return true;
        }

        var meaningfulTerms = SearchTermPattern()
            .Matches(keyword)
            .Select(match => match.Value)
            .Where(term => term.Length >= 3)
            .ToArray();

        var availableTerms = SearchTermPattern()
            .Matches(searchableText)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return meaningfulTerms.Any(availableTerms.Contains);
    }

    private static bool ContainsWholePhrase(string text, string phrase)
    {
        var trimmedPhrase = phrase.Trim();
        if (trimmedPhrase.Length == 0)
        {
            return false;
        }

        var pattern =
            $@"(?<![\p{{L}}\p{{N}}+#.]){Regex.Escape(trimmedPhrase)}(?![\p{{L}}\p{{N}}+#.])";

        return Regex.IsMatch(
            text,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex MarkupPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpacePattern();

    [GeneratedRegex(@"[\p{L}\p{N}+#.]+")]
    private static partial Regex SearchTermPattern();

    [GeneratedRegex(
        @"(?:^|[\s,;/()\-])(brasil|brazil|s[aã]o paulo|rio de janeiro|belo horizonte|curitiba|porto alegre|bras[ií]lia|recife|salvador|campinas|florian[oó]polis|manaus|fortaleza|goi[aâ]nia|vit[oó]ria|barueri|osasco|amazonas|bahia|cear[aá]|esp[ií]rito santo|goi[aá]s|maranh[aã]o|paran[aá]|pernambuco|santa catarina|rio grande do sul)(?:$|[\s,;/()\-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BrazilPattern();

    [GeneratedRegex(@"\b(hybrid|h[ií]brid[oa])\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HybridPattern();

    [GeneratedRegex(@"\b(remote|remot[oa]|home office)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RemotePattern();

    [GeneratedRegex(@"\b(on[ -]?site|presencial)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OnSitePattern();

    [GeneratedRegex(@"\b(intern(ship)?|est[aá]gio|estagi[aá]ri[oa])\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InternshipPattern();

    [GeneratedRegex(@"\b(part[ -]?time|meio per[ií]odo)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PartTimePattern();

    [GeneratedRegex(@"\b(contract(or)?|pj)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContractPattern();

    [GeneratedRegex(@"\b(temporary|tempor[aá]ri[oa])\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TemporaryPattern();

    [GeneratedRegex(@"\b(freelance|freelancer)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FreelancePattern();

    [GeneratedRegex(@"\b(full[ -]?time|tempo integral|permanent)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FullTimePattern();
}
