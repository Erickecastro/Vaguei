using System.Globalization;
using System.Text;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class ResumeSectionClassifier
{
    private static readonly IReadOnlyDictionary<string, SkillEvidenceSource>
        SectionTitles = CreateSectionTitles();

    public IReadOnlyDictionary<SkillEvidenceSource, string> Classify(
        string resumeText)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return new Dictionary<SkillEvidenceSource, string>();
        }

        var sections =
            new Dictionary<SkillEvidenceSource, List<string>>();

        SkillEvidenceSource? currentSection = null;

        foreach (var line in resumeText.Split(
                     Environment.NewLine,
                     StringSplitOptions.RemoveEmptyEntries |
                     StringSplitOptions.TrimEntries))
        {
            if (TryGetSection(line, out var section))
            {
                currentSection = section;
                continue;
            }

            if (currentSection is null)
            {
                continue;
            }

            if (!sections.TryGetValue(
                    currentSection.Value,
                    out var lines))
            {
                lines = [];
                sections[currentSection.Value] = lines;
            }

            lines.Add(line);
        }

        return sections.ToDictionary(
            section => section.Key,
            section => string.Join(
                Environment.NewLine,
                section.Value));
    }

    private static bool TryGetSection(
        string line,
        out SkillEvidenceSource section)
    {
        return SectionTitles.TryGetValue(
            Normalize(line),
            out section);
    }

    private static IReadOnlyDictionary<string, SkillEvidenceSource>
        CreateSectionTitles()
    {
        var titles =
            new Dictionary<string, SkillEvidenceSource>(
                StringComparer.OrdinalIgnoreCase);

        AddTitles(
            titles,
            SkillEvidenceSource.ProfessionalSummary,
            "resumo profissional",
            "perfil profissional",
            "sobre mim",
            "sobre",
            "professional summary",
            "profile",
            "about me");

        AddTitles(
            titles,
            SkillEvidenceSource.ExperienceDescription,
            "experiencia profissional",
            "experiencias profissionais",
            "experiencia",
            "professional experience",
            "work experience",
            "experience");

        AddTitles(
            titles,
            SkillEvidenceSource.SkillsSection,
            "habilidades",
            "competencias",
            "linguagens e tecnologias",
            "tecnologias",
            "skills",
            "technical skills",
            "core competencies");

        AddTitles(
            titles,
            SkillEvidenceSource.Project,
            "projetos",
            "projetos pessoais",
            "projects",
            "personal projects");

        AddTitles(
            titles,
            SkillEvidenceSource.Education,
            "formacao academica",
            "formacao",
            "educacao",
            "education",
            "academic background");

        AddTitles(
            titles,
            SkillEvidenceSource.Course,
            "cursos",
            "cursos complementares",
            "courses",
            "training");

        AddTitles(
            titles,
            SkillEvidenceSource.Certification,
            "certificacoes",
            "certifications",
            "licenses and certifications");

        AddTitles(
            titles,
            SkillEvidenceSource.LanguageSection,
            "idiomas",
            "languages");

        return titles;
    }

    private static void AddTitles(
        IDictionary<string, SkillEvidenceSource> titles,
        SkillEvidenceSource source,
        params string[] sectionTitles)
    {
        foreach (var title in sectionTitles)
        {
            titles[Normalize(title)] = source;
        }
    }

    private static string Normalize(string value)
    {
        var normalized = value
            .Trim()
            .TrimEnd(':')
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
