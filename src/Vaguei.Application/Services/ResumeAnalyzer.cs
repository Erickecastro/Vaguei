using Vaguei.Application.Catalogs;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class ResumeAnalyzer
{
    private readonly SkillMatcher _skillMatcher;
    private readonly WorkExperienceExtractor _experienceExtractor;
    private readonly ProfessionalSummaryExtractor _summaryExtractor;
    private readonly SkillRelevanceAnalyzer _skillRelevanceAnalyzer;
    private readonly ResumeSectionClassifier _sectionClassifier;
    private readonly ResumeTextSanitizer _textSanitizer;

    public ResumeAnalyzer()
    {
        _skillMatcher = new SkillMatcher();
        _experienceExtractor = new WorkExperienceExtractor();
        _summaryExtractor = new ProfessionalSummaryExtractor();
        _skillRelevanceAnalyzer = new SkillRelevanceAnalyzer();
        _sectionClassifier = new ResumeSectionClassifier();
        _textSanitizer = new ResumeTextSanitizer();
    }

    public CandidateProfile Analyze(string resumeText)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            throw new ArgumentException(
                "O texto do currículo não pode estar vazio.",
                nameof(resumeText));
        }

        var sanitizedText = _textSanitizer.Sanitize(
            resumeText);

        var lines = sanitizedText
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var profile = new CandidateProfile();

        profile.Name = ExtractName(lines);
        profile.ProfessionalTitle = ExtractProfessionalTitle(lines);
        profile.Summary = _summaryExtractor.Extract(sanitizedText);

        var matchedSkills = ExtractSkills(sanitizedText).ToArray();

        foreach (var skill in matchedSkills)
        {
            profile.Skills.Add(skill.Name);
        }

        foreach (var experience in _experienceExtractor.Extract(sanitizedText))
        {
            profile.Experiences.Add(experience);
        }

        var evidenceBySkill = CollectEvidence(
            profile,
            matchedSkills,
            _sectionClassifier.Classify(sanitizedText));

        foreach (var detailedSkill in
            _skillRelevanceAnalyzer.Analyze(
                profile,
                evidenceBySkill))
        {
            profile.DetailedSkills.Add(detailedSkill);
        }

        return profile;
    }

    private static string ExtractName(string[] lines)
    {
        foreach (var line in lines)
        {
            if (line.Contains('@') ||
                line.StartsWith('+') ||
                line.StartsWith(
                    "http",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.All(character =>
                    char.IsLetter(character) ||
                    char.IsWhiteSpace(character)))
            {
                return line;
            }
        }

        return string.Empty;
    }

    private static string ExtractProfessionalTitle(string[] lines)
    {
        var name = ExtractName(lines);

        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var nameIndex =
            Array.IndexOf(
                lines,
                name);

        if (nameIndex >= 0 &&
            nameIndex + 1 < lines.Length)
        {
            return lines[nameIndex + 1];
        }

        return string.Empty;
    }

    private IEnumerable<SkillDefinition> ExtractSkills(
        string resumeText)
    {
        foreach (var skill in SkillCatalog.Skills)
        {
            if (_skillMatcher.ContainsSkill(
                    resumeText,
                    skill))
            {
                yield return skill;
            }
        }
    }

    private IReadOnlyDictionary<string, IReadOnlyCollection<SkillEvidence>>
        CollectEvidence(
            CandidateProfile profile,
            IEnumerable<SkillDefinition> skills,
            IReadOnlyDictionary<SkillEvidenceSource, string> sections)
    {
        var result =
            new Dictionary<string, IReadOnlyCollection<SkillEvidence>>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var skill in skills)
        {
            var evidence = sections
                .Where(section =>
                    section.Key is not SkillEvidenceSource.ProfessionalSummary and
                    not SkillEvidenceSource.ExperienceDescription)
                .Where(section =>
                    _skillMatcher.ContainsSkill(
                        section.Value,
                        skill))
                .Select(section =>
                    new SkillEvidence(
                        section.Key))
                .ToHashSet();

            AddEvidenceWhenMatched(
                evidence,
                profile.ProfessionalTitle,
                skill,
                SkillEvidenceSource.ProfessionalTitle);

            AddEvidenceWhenMatched(
                evidence,
                profile.Summary,
                skill,
                SkillEvidenceSource.ProfessionalSummary);

            foreach (var experience in profile.Experiences)
            {
                AddEvidenceWhenMatched(
                    evidence,
                    experience.Position,
                    skill,
                    SkillEvidenceSource.ExperiencePosition);

                AddEvidenceWhenMatched(
                    evidence,
                    experience.Description,
                    skill,
                    SkillEvidenceSource.ExperienceDescription);
            }

            result[skill.Name] = evidence;
        }

        return result;
    }

    private void AddEvidenceWhenMatched(
        ISet<SkillEvidence> evidence,
        string? text,
        SkillDefinition skill,
        SkillEvidenceSource source)
    {
        if (!string.IsNullOrWhiteSpace(text) &&
            _skillMatcher.ContainsSkill(
                text,
                skill))
        {
            evidence.Add(
                new SkillEvidence(source));
        }
    }
}
