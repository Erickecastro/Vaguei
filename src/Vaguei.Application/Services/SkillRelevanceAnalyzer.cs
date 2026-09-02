using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class SkillRelevanceAnalyzer
{
    private const int ProfessionalTitleWeight = 4;
    private const int SummaryWeight = 2;
    private const int ExperiencePositionWeight = 2;
    private const int ExperienceDescriptionWeight = 2;

    private const int PrimaryThreshold = 4;
    private const int RelevantThreshold = 2;

    public IReadOnlyCollection<CandidateSkill> Analyze(
        CandidateProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var skills =
            new List<CandidateSkill>();

        foreach (var skill in profile.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill))
            {
                continue;
            }

            var score =
                CalculateRelevanceScore(
                    profile,
                    skill);

            var relevance =
                GetRelevance(score);

            skills.Add(
                new CandidateSkill(
                    skill,
                    relevance));
        }

        return skills;
    }

    private static int CalculateRelevanceScore(
        CandidateProfile profile,
        string skill)
    {
        var score = 0;

        if (ContainsTerm(
                profile.ProfessionalTitle,
                skill))
        {
            score +=
                ProfessionalTitleWeight;
        }

        if (ContainsTerm(
                profile.Summary,
                skill))
        {
            score +=
                SummaryWeight;
        }

        foreach (var experience in profile.Experiences)
        {
            if (ContainsTerm(
                    experience.Position,
                    skill))
            {
                score +=
                    ExperiencePositionWeight;
            }

            if (ContainsTerm(
                    experience.Description,
                    skill))
            {
                score +=
                    ExperienceDescriptionWeight;
            }
        }

        return score;
    }

    private static SkillRelevance GetRelevance(
        int score)
    {
        if (score >= PrimaryThreshold)
        {
            return SkillRelevance.Primary;
        }

        if (score >= RelevantThreshold)
        {
            return SkillRelevance.Relevant;
        }

        return SkillRelevance.Supporting;
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

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC);
    }
}