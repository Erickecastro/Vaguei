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

    private const int MaximumSkillEvidence = 4;

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

        if (profile.Skills.Count > 0)
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

        var score =
            Math.Round(
                weightedScore /
                availableWeight *
                100,
                2);

        return new JobMatchResult(
            job,
            score,
            reasons);
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

    private static double CalculateRoleScore(
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

    private static double CalculateRoleSimilarity(
        string expectedRole,
        string jobTitle)
    {
        var expectedText =
            NormalizeText(
                expectedRole);

        var jobText =
            NormalizeText(
                jobTitle);

        if (string.IsNullOrWhiteSpace(
                expectedText) ||
            string.IsNullOrWhiteSpace(
                jobText))
        {
            return 0;
        }

        if (expectedText.Equals(
                jobText,
                StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (jobText.Contains(
                expectedText,
                StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var expectedTokens =
            Tokenize(expectedText);

        var jobTokens =
            Tokenize(jobText);

        if (expectedTokens.Count == 0)
        {
            return 0;
        }

        var matchedTokens =
            expectedTokens.Count(
                jobTokens.Contains);

        return
            (double)matchedTokens /
            expectedTokens.Count;
    }

    private static IReadOnlyCollection<string>
        FindMatchedSkills(
            CandidateProfile profile,
            JobPosting job)
    {
        var matchedSkills =
            new List<string>();

        foreach (var skill in profile.Skills)
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

    private static bool JobContainsSkill(
        JobPosting job,
        string skill)
    {
        if (job.Skills.Contains(
                skill))
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

    private static double CalculateSkillScore(
        CandidateProfile profile,
        IReadOnlyCollection<string> matchedSkills)
    {
        var requiredEvidence =
            Math.Min(
                profile.Skills.Count,
                MaximumSkillEvidence);

        if (requiredEvidence == 0)
        {
            return 0;
        }

        return Math.Min(
            1,
            (double)matchedSkills.Count /
            requiredEvidence);
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

    private static HashSet<string> Tokenize(
        string text)
    {
        return Regex
            .Matches(
                text,
                @"[\p{L}\p{N}+#.]+")
            .Select(
                match =>
                    match.Value)
            .Where(
                token =>
                    !string.IsNullOrWhiteSpace(
                        token))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);
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