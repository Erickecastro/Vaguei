using Vaguei.Application.Catalogs;
using Vaguei.Domain.Entities;

namespace Vaguei.Application.Services;

public sealed class ResumeAnalyzer
{
    private readonly SkillMatcher _skillMatcher;
    private readonly WorkExperienceExtractor _experienceExtractor;

    public ResumeAnalyzer()
    {
        _skillMatcher = new SkillMatcher();
        _experienceExtractor = new WorkExperienceExtractor();
    }

    public CandidateProfile Analyze(string resumeText)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            throw new ArgumentException(
                "O texto do currículo não pode estar vazio.",
                nameof(resumeText));
        }

        var lines = resumeText
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var profile = new CandidateProfile();

        profile.Name = ExtractName(lines);
        profile.ProfessionalTitle = ExtractProfessionalTitle(lines);

        foreach (var skill in ExtractSkills(resumeText))
        {
            profile.Skills.Add(skill);
        }

        foreach (var experience in _experienceExtractor.Extract(resumeText))
        {
            profile.Experiences.Add(experience);
        }

        return profile;
    }

    private static string ExtractName(string[] lines)
    {
        foreach (var line in lines)
        {
            if (line.Contains('@') ||
                line.StartsWith('+') ||
                line.StartsWith("http", StringComparison.OrdinalIgnoreCase))
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

        var nameIndex = Array.IndexOf(lines, name);

        if (nameIndex >= 0 &&
            nameIndex + 1 < lines.Length)
        {
            return lines[nameIndex + 1];
        }

        return string.Empty;
    }

    private IEnumerable<string> ExtractSkills(string resumeText)
    {
        foreach (var skill in SkillCatalog.Skills)
        {
            if (_skillMatcher.ContainsSkill(resumeText, skill))
            {
                yield return skill.Name;
            }
        }
    }
}