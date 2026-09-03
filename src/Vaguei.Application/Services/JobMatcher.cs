using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobMatcher
{
    private const double RoleWeight = 50;
    private const double SkillWeight = 50;
    private const double PrimarySkillWeight = 5;
    private const double RelevantSkillWeight = 2;
    private const double SupportingSkillWeight = 1;
    private const double UnspecifiedSkillWeight = 1;
    private const int MaximumWeightedSkillEvidence = 4;
    private const double CoreRequirementPenalty = 15;
    private const double RequiredRequirementPenalty = 8;
    private const double MaximumRequirementPenalty = 25;

    public JobMatchResult Match(
        CandidateProfile profile,
        JobPosting job,
        JobSearchPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(preferences);

        var reasons =
            new List<JobMatchReason>();

        var weightedScore = 0.0;
        var availableWeight = 0.0;

        var roleCandidates =
            GetRoleCandidates(
                profile,
                preferences);

        if (roleCandidates.Count > 0 &&
            !string.IsNullOrWhiteSpace(job.Title))
        {
            availableWeight +=
                RoleWeight;

            var roleScore =
                CalculateRoleScore(
                    roleCandidates,
                    job.Title);

            weightedScore +=
                roleScore * RoleWeight;

            AddRoleReason(
                reasons,
                roleScore);
        }

        if (profile.DetailedSkills.Count > 0 ||
            profile.Skills.Count > 0)
        {
            availableWeight +=
                SkillWeight;

            var matchedSkills =
                FindMatchedSkills(
                    profile,
                    job);

            var skillScore =
                CalculateSkillScore(
                    profile,
                    matchedSkills);

            weightedScore +=
                skillScore * SkillWeight;

            AddSkillReasons(
                reasons,
                matchedSkills);
        }

        if (availableWeight == 0)
        {
            reasons.Add(
                new JobMatchReason
                {
                    Criterion =
                        JobMatchCriterion.Other,

                    Kind =
                        JobMatchReasonKind.Neutral,

                    Description =
                        "Não há informações suficientes para calcular a compatibilidade."
                });

            return new JobMatchResult(
                job,
                0,
                reasons);
        }

        var baseScore =
            weightedScore /
            availableWeight *
            100;

        var requirementPenalty =
            CalculateRequirementPenalty(
                profile,
                job,
                reasons);

        var score = Math.Round(
            Math.Max(
                0,
                baseScore - requirementPenalty),
            2);

        return new JobMatchResult(
            job,
            score,
            reasons);
    }

    private readonly ProfessionalRoleNormalizer
        _roleNormalizer;

    public JobMatcher()
        : this(
            new ProfessionalRoleNormalizer())
    {
    }

    public JobMatcher(
        ProfessionalRoleNormalizer roleNormalizer)
    {
        ArgumentNullException.ThrowIfNull(
            roleNormalizer);

        _roleNormalizer =
            roleNormalizer;
    }

    private static IReadOnlyCollection<string>
        GetRoleCandidates(
            CandidateProfile profile,
            JobSearchPreferences preferences)
    {
        if (preferences.DesiredRoles.Count > 0)
        {
            return preferences
                .DesiredRoles
                .Where(
                    role =>
                        !string.IsNullOrWhiteSpace(
                            role))
                .ToArray();
        }

        if (string.IsNullOrWhiteSpace(
                profile.ProfessionalTitle))
        {
            return [];
        }

        return
        [
            profile.ProfessionalTitle
        ];
    }

    private double CalculateRoleScore(
        IEnumerable<string> roleCandidates,
        string jobTitle)
    {
        return roleCandidates
            .Select(
                role =>
                    CalculateRoleSimilarity(
                        role,
                        jobTitle))
            .DefaultIfEmpty(0)
            .Max();
    }

    private double CalculateRoleSimilarity(
        string expectedRole,
        string jobTitle)
    {
        var expectedTerms =
            _roleNormalizer.Normalize(
                expectedRole);

        var jobTerms =
            _roleNormalizer.Normalize(
                jobTitle);

        if (expectedTerms.Count == 0 ||
            jobTerms.Count == 0)
        {
            return 0;
        }

        var jobTermSet =
            jobTerms.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var matchedTerms =
            expectedTerms.Count(
                jobTermSet.Contains);

        return
            (double)matchedTerms /
            expectedTerms.Count;
    }

    private static IReadOnlyCollection<string>
        FindMatchedSkills(
            CandidateProfile profile,
            JobPosting job)
    {
        var candidateSkills =
            GetCandidateSkillNames(
                profile);

        var matchedSkills =
            new List<string>();

        foreach (var skill in candidateSkills)
        {
            if (JobContainsSkill(
                    job,
                    skill))
            {
                matchedSkills.Add(
                    skill);
            }
        }

        return matchedSkills;
    }

    private static IReadOnlyCollection<string>
        GetCandidateSkillNames(
            CandidateProfile profile)
    {
        if (profile.DetailedSkills.Count > 0)
        {
            return profile
                .DetailedSkills
                .Select(
                    skill =>
                        skill.Name)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return profile
            .Skills
            .Where(
                skill =>
                    !string.IsNullOrWhiteSpace(
                        skill))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool JobContainsSkill(
        JobPosting job,
        string skill)
    {
        if (job.Skills.Contains(
                skill))
        {
            return true;
        }

        if (job.SkillRequirements.Any(requirement =>
                requirement.Name.Equals(
                    skill,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return
            ContainsTerm(
                job.Title,
                skill) ||
            ContainsTerm(
                job.Description,
                skill);
    }

    private static double CalculateSkillScore(CandidateProfile profile, IReadOnlyCollection<string> matchedSkills)
    {
        if (profile.DetailedSkills.Count > 0)
        {
            return CalculateWeightedSkillScore(
                profile.DetailedSkills,
                matchedSkills);
        }

        return CalculateLegacySkillScore(
            profile.Skills,
            matchedSkills);
    }

    private static double CalculateWeightedSkillScore(
        IEnumerable<CandidateSkill> candidateSkills,
        IReadOnlyCollection<string> matchedSkills)
    {
        var matchedSkillSet =
            matchedSkills.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var skills =
            candidateSkills
                .GroupBy(
                    skill =>
                        skill.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    group =>
                        group.First())
                .ToArray();

        if (skills.Length == 0)
        {
            return 0;
        }

        var requiredEvidenceWeight =
            skills
                .Select(
                    skill =>
                        GetSkillWeight(
                            skill.Relevance))
                .OrderByDescending(
                    weight =>
                        weight)
                .Take(
                    MaximumWeightedSkillEvidence)
                .Sum();

        if (requiredEvidenceWeight <= 0)
        {
            return 0;
        }

        var matchedWeight =
            skills
                .Where(
                    skill =>
                        matchedSkillSet.Contains(
                            skill.Name))
                .Sum(
                    skill =>
                        GetSkillWeight(
                            skill.Relevance));

        return Math.Min(
            1,
            matchedWeight /
            requiredEvidenceWeight);
    }

    private static double CalculateLegacySkillScore(
        IReadOnlyCollection<string> candidateSkills,
        IReadOnlyCollection<string> matchedSkills)
    {
        const int maximumSkillEvidence = 4;

        var requiredEvidence =
            Math.Min(
                candidateSkills.Count,
                maximumSkillEvidence);

        if (requiredEvidence == 0)
        {
            return 0;
        }

        return Math.Min(
            1,
            (double)matchedSkills.Count /
            requiredEvidence);
    }

    private static double GetSkillWeight(
        SkillRelevance relevance)
    {
        return relevance switch
        {
            SkillRelevance.Primary =>
                PrimarySkillWeight,

            SkillRelevance.Relevant =>
                RelevantSkillWeight,

            SkillRelevance.Supporting =>
                SupportingSkillWeight,

            SkillRelevance.Unspecified =>
                UnspecifiedSkillWeight,

            _ =>
                UnspecifiedSkillWeight
        };
    }

    private static double CalculateRequirementPenalty(
        CandidateProfile profile,
        JobPosting job,
        ICollection<JobMatchReason> reasons)
    {
        var candidateSkills = GetCandidateSkillNames(profile)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (candidateSkills.Count == 0 ||
            job.SkillRequirements.Count == 0)
        {
            return 0;
        }

        var missingRequirements = job.SkillRequirements
            .Where(requirement =>
                requirement.Level is
                    JobSkillRequirementLevel.Core or
                    JobSkillRequirementLevel.Required)
            .Where(requirement =>
                !candidateSkills.Contains(requirement.Name))
            .GroupBy(
                requirement => requirement.Name,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
                group.OrderByDescending(
                        requirement => requirement.Level)
                    .First())
            .ToArray();

        foreach (var requirement in missingRequirements)
        {
            reasons.Add(
                new JobMatchReason
                {
                    Criterion = JobMatchCriterion.Skill,
                    Kind = JobMatchReasonKind.Negative,
                    Description = requirement.Level ==
                        JobSkillRequirementLevel.Core
                            ? $"Competência central não identificada no perfil: {requirement.Name}."
                            : $"Competência obrigatória não identificada no perfil: {requirement.Name}."
                });
        }

        var penalty = missingRequirements.Sum(requirement =>
            requirement.Level == JobSkillRequirementLevel.Core
                ? CoreRequirementPenalty
                : RequiredRequirementPenalty);

        return Math.Min(
            MaximumRequirementPenalty,
            penalty);
    }

    private static void AddRoleReason(
        ICollection<JobMatchReason> reasons,
        double roleScore)
    {
        if (roleScore > 0)
        {
            reasons.Add(
                new JobMatchReason
                {
                    Criterion =
                        JobMatchCriterion.ProfessionalRole,

                    Kind =
                        JobMatchReasonKind.Positive,

                    Description =
                        "O cargo da vaga possui relação com o cargo profissional desejado."
                });

            return;
        }

        reasons.Add(
            new JobMatchReason
            {
                Criterion =
                    JobMatchCriterion.ProfessionalRole,

                Kind =
                    JobMatchReasonKind.Neutral,

                Description =
                    "Não foram encontrados termos em comum entre os cargos."
            });
    }

    private static void AddSkillReasons(
        ICollection<JobMatchReason> reasons,
        IEnumerable<string> matchedSkills)
    {
        var matches =
            matchedSkills.ToArray();

        if (matches.Length == 0)
        {
            reasons.Add(
                new JobMatchReason
                {
                    Criterion =
                        JobMatchCriterion.Skill,

                    Kind =
                        JobMatchReasonKind.Neutral,

                    Description =
                        "Nenhuma competência do perfil foi identificada na vaga."
                });

            return;
        }

        foreach (var skill in matches)
        {
            reasons.Add(
                new JobMatchReason
                {
                    Criterion =
                        JobMatchCriterion.Skill,

                    Kind =
                        JobMatchReasonKind.Positive,

                    Description =
                        $"Competência compatível: {skill}."
                });
        }
    }

    private static bool ContainsTerm(
        string? text,
        string term)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var normalizedText =
            NormalizeText(text);

        var normalizedTerm =
            NormalizeText(term);

        if (string.IsNullOrWhiteSpace(
                normalizedTerm))
        {
            return false;
        }

        var pattern =
            $@"(?<![\p{{L}}\p{{N}}])" +
            Regex.Escape(normalizedTerm) +
            @"(?![\p{L}\p{N}])";

        return Regex.IsMatch(
            normalizedText,
            pattern,
            RegexOptions.IgnoreCase);
    }

    private static string NormalizeText(
        string text)
    {
        var decomposed =
            text
                .Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in decomposed)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(
                    character);
            }
        }

        return Regex.Replace(
                builder
                    .ToString()
                    .Normalize(
                        NormalizationForm.FormC),
                @"\s+",
                " ")
            .Trim();
    }
}
