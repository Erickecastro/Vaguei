using System.Text.RegularExpressions;
using Vaguei.Application.Catalogs;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobSkillRequirementAnalyzer
{
    private static readonly string[] RequiredMarkers =
    [
        "obrigatório",
        "obrigatória",
        "obrigatórios",
        "obrigatórias",
        "requisito",
        "requisitos",
        "necessário",
        "necessária",
        "essencial",
        "indispensável",
        "required",
        "requirement",
        "requirements",
        "must have",
        "essential",
        "mandatory"
    ];

    private static readonly string[] PreferredMarkers =
    [
        "desejável",
        "desejáveis",
        "diferencial",
        "preferencial",
        "preferred",
        "nice to have",
        "desirable",
        "bonus"
    ];

    private readonly SkillMatcher _skillMatcher = new();

    public IReadOnlyCollection<JobSkillRequirement> Analyze(
        JobPosting job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var definitions = GetSkillDefinitions(job);
        var requirements = new List<JobSkillRequirement>();

        foreach (var definition in definitions)
        {
            var level = GetRequirementLevel(
                job,
                definition);

            if (level is null)
            {
                continue;
            }

            requirements.Add(
                new JobSkillRequirement(
                    definition.Name,
                    level.Value));
        }

        return requirements;
    }

    private JobSkillRequirementLevel? GetRequirementLevel(
        JobPosting job,
        SkillDefinition skill)
    {
        if (_skillMatcher.ContainsSkill(
                job.Title,
                skill))
        {
            return JobSkillRequirementLevel.Core;
        }

        var matchingContexts = SplitContexts(job.Description)
            .Where(context =>
                _skillMatcher.ContainsSkill(
                    context,
                    skill))
            .ToArray();

        if (matchingContexts.Any(context =>
                ContainsMarker(
                    context,
                    RequiredMarkers)))
        {
            return JobSkillRequirementLevel.Required;
        }

        if (matchingContexts.Any(context =>
                ContainsMarker(
                    context,
                    PreferredMarkers)))
        {
            return JobSkillRequirementLevel.Preferred;
        }

        if (matchingContexts.Length > 0 ||
            job.Skills.Any(structuredSkill =>
                _skillMatcher.ContainsSkill(
                    structuredSkill,
                    skill)) ||
            job.Tags.Any(tag =>
                _skillMatcher.ContainsSkill(
                    tag,
                    skill)))
        {
            return JobSkillRequirementLevel.Mentioned;
        }

        return null;
    }

    private IReadOnlyCollection<SkillDefinition> GetSkillDefinitions(
        JobPosting job)
    {
        var definitions = SkillCatalog.Skills.ToList();
        var knownDefinitions = definitions.ToArray();

        definitions.AddRange(
            job.Skills
                .Where(skill =>
                    !string.IsNullOrWhiteSpace(skill) &&
                    !knownDefinitions.Any(definition =>
                        _skillMatcher.ContainsSkill(
                            skill,
                            definition)))
                .Select(skill =>
                    new SkillDefinition
                    {
                        Name = skill,
                        Category = SkillCategory.Unknown
                    }));

        return definitions;
    }

    private static IEnumerable<string> SplitContexts(
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return [];
        }

        return Regex.Split(
                description,
                @"(?<=[.!?;])\s+|[\r\n]+")
            .Where(context =>
                !string.IsNullOrWhiteSpace(context));
    }

    private static bool ContainsMarker(
        string context,
        IEnumerable<string> markers)
    {
        return markers.Any(marker =>
            context.Contains(
                marker,
                StringComparison.OrdinalIgnoreCase));
    }
}
