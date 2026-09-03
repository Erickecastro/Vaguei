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
    private const int SkillsSectionWeight = 2;
    private const int ProjectWeight = 2;
    private const int CertificationWeight = 2;
    private const int SupportingContextWeight = 1;

    private const int PrimaryThreshold = 4;
    private const int RelevantThreshold = 2;

    public IReadOnlyCollection<CandidateSkill> Analyze(
        CandidateProfile profile)
    {
        return Analyze(
            profile,
            new Dictionary<string, IReadOnlyCollection<SkillEvidence>>(
                StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyCollection<CandidateSkill> Analyze(
        CandidateProfile profile,
        IReadOnlyDictionary<
            string,
            IReadOnlyCollection<SkillEvidence>> evidenceBySkill)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(evidenceBySkill);

        var skills =
            new List<CandidateSkill>();

        foreach (var skill in profile.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill))
            {
                continue;
            }

            var evidence = GetEvidence(
                profile,
                skill,
                evidenceBySkill);

            var score = evidence.Sum(
                item => GetEvidenceWeight(item.Source));

            var relevance =
                GetRelevance(score);

            skills.Add(
                new CandidateSkill(
                    skill,
                    relevance,
                    evidence));
        }

        return skills;
    }

    private static IReadOnlyCollection<SkillEvidence> GetEvidence(
        CandidateProfile profile,
        string skill,
        IReadOnlyDictionary<
            string,
            IReadOnlyCollection<SkillEvidence>> evidenceBySkill)
    {
        var evidence = new HashSet<SkillEvidence>();

        if (evidenceBySkill.TryGetValue(skill, out var providedEvidence))
        {
            evidence.UnionWith(providedEvidence);
        }

        if (ContainsTerm(
                profile.ProfessionalTitle,
                skill))
        {
            evidence.Add(
                new SkillEvidence(
                    SkillEvidenceSource.ProfessionalTitle));
        }

        if (ContainsTerm(
                profile.Summary,
                skill))
        {
            evidence.Add(
                new SkillEvidence(
                    SkillEvidenceSource.ProfessionalSummary));
        }

        foreach (var experience in profile.Experiences)
        {
            if (ContainsTerm(
                    experience.Position,
                    skill))
            {
                evidence.Add(
                    new SkillEvidence(
                        SkillEvidenceSource.ExperiencePosition));
            }

            if (ContainsTerm(
                    experience.Description,
                    skill))
            {
                evidence.Add(
                    new SkillEvidence(
                        SkillEvidenceSource.ExperienceDescription));
            }
        }

        return evidence.ToArray();
    }

    private static int GetEvidenceWeight(
        SkillEvidenceSource source)
    {
        return source switch
        {
            SkillEvidenceSource.ProfessionalTitle =>
                ProfessionalTitleWeight,
            SkillEvidenceSource.ProfessionalSummary =>
                SummaryWeight,
            SkillEvidenceSource.ExperiencePosition =>
                ExperiencePositionWeight,
            SkillEvidenceSource.ExperienceDescription =>
                ExperienceDescriptionWeight,
            SkillEvidenceSource.SkillsSection =>
                SkillsSectionWeight,
            SkillEvidenceSource.Project =>
                ProjectWeight,
            SkillEvidenceSource.Certification =>
                CertificationWeight,
            SkillEvidenceSource.Education or
            SkillEvidenceSource.Course or
            SkillEvidenceSource.LanguageSection or
            SkillEvidenceSource.Other =>
                SupportingContextWeight,
            _ => SupportingContextWeight
        };
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
