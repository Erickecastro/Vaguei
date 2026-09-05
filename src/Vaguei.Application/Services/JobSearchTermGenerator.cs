using Vaguei.Application.Catalogs;
using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobSearchTermGenerator
{
    private const int MaxSkillTerms = 4;

    private static readonly HashSet<SkillCategory>
        SearchableSkillCategories =
        [
            SkillCategory.Language,
            SkillCategory.Backend,
            SkillCategory.Frontend,
            SkillCategory.Mobile
        ];

    private static readonly (Regex Pattern, string[] Variants)[]
        DesiredRoleVariants =
        [
            (new Regex(@"\b(est[aá]gio|estagi[aá]ri[oa]|intern(ship)?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["estagio", "estagiário", "estagiaria", "internship", "intern"]),
            (new Regex(@"\b(analista de dados|data analyst)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["analista de dados", "data analyst"]),
            (new Regex(@"\b(cientista de dados|data scientist)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["cientista de dados", "data scientist"]),
            (new Regex(@"\b(engenheir[oa] de software|software engineer)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["engenheiro de software", "software engineer", "software developer"]),
            (new Regex(@"\bdesenvolvedor[a]?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["desenvolvedor", "developer"]),
            (new Regex(@"\b(recursos humanos|human resources)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["recursos humanos", "human resources", "people operations"]),
            (new Regex(@"\b(contador[a]?|accountant)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["contador", "accountant"]),
            (new Regex(@"\b(enfermeir[oa]|nurse)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["enfermeiro", "nurse"]),
            (new Regex(@"\b(log[ií]stica|logistics)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                ["logística", "logistics"])
        ];

    public IReadOnlyCollection<string> Generate(
        CandidateProfile profile,
        JobSearchPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(preferences);

        var terms =
            new List<string>();

        if (preferences.DesiredRoles.Count > 0)
        {
            foreach (var role in preferences.DesiredRoles)
            {
                AddDistinct(
                    terms,
                    role);

                AddDesiredRoleVariants(
                    terms,
                    role);
            }

            return terms;
        }

        AddDistinct(
            terms,
            profile.ProfessionalTitle);

        AddRoleVariants(
            terms,
            profile);

        AddSkillTerms(
            terms,
            profile);

        return terms;
    }

    private static void AddDesiredRoleVariants(
        List<string> terms,
        string role)
    {
        foreach (var expansion in DesiredRoleVariants)
        {
            if (!expansion.Pattern.IsMatch(role))
            {
                continue;
            }

            foreach (var variant in expansion.Variants)
            {
                AddDistinct(terms, variant);
            }
        }
    }

    private static void AddRoleVariants(
        List<string> terms,
        CandidateProfile profile)
    {
        if (HasSkill(profile, ".NET"))
        {
            AddDistinct(
                terms,
                ".NET Developer");

            AddDistinct(
                terms,
                "Software Engineer");
        }

        if (HasSkill(profile, "C#"))
        {
            AddDistinct(
                terms,
                "C# Developer");
        }

        if (HasSkill(profile, "ASP.NET Core"))
        {
            AddDistinct(
                terms,
                "Backend Developer");
        }

        if (HasSkill(profile, "Node.js"))
        {
            AddDistinct(
                terms,
                "Node.js Developer");
        }

        if (HasSkill(profile, "React"))
        {
            AddDistinct(
                terms,
                "React Developer");
        }
    }

    private static void AddSkillTerms(
        List<string> terms,
        CandidateProfile profile)
    {
        var skillTerms =
            SkillCatalog.Skills
                .Where(
                    skill =>
                        SearchableSkillCategories.Contains(
                            skill.Category))
                .Where(
                    skill =>
                        HasSkill(
                            profile,
                            skill.Name))
                .Take(MaxSkillTerms);

        foreach (var skill in skillTerms)
        {
            AddDistinct(
                terms,
                skill.Name);
        }
    }

    private static bool HasSkill(
        CandidateProfile profile,
        string skill)
    {
        return profile.Skills.Contains(skill);
    }

    private static void AddDistinct(
        List<string> terms,
        string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        var normalizedTerm =
            term.Trim();

        if (terms.Any(
                existing =>
                    existing.Equals(
                        normalizedTerm,
                        StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        terms.Add(normalizedTerm);
    }
}
