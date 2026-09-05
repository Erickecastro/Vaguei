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
        if (!Regex.IsMatch(
                role,
                @"\b(est[aá]gio|estagi[aá]ri[oa]|intern(ship)?)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return;
        }

        AddDistinct(terms, "estagio");
        AddDistinct(terms, "estagiário");
        AddDistinct(terms, "estagiaria");
        AddDistinct(terms, "internship");
        AddDistinct(terms, "intern");
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
