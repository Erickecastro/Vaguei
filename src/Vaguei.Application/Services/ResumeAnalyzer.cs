using Vaguei.Domain.Entities;

namespace Vaguei.Application.Services;

public sealed class ResumeAnalyzer
{
    private static readonly string[] KnownSkills =
    [
        "C#",
        ".NET",
        "ASP.NET Core",
        ".NET MAUI",
        "Entity Framework Core",
        "PostgreSQL",
        "SQLite",
        "SQL",
        "JavaScript",
        "Node.js",
        "Express",
        "React",
        "Vite",
        "HTML",
        "CSS",
        "Tailwind",
        "Git",
        "GitHub",
        "Docker",
        "Swagger",
        "OpenAPI",
        "JWT",
        "REST",
        "MVVM",
        "SOLID",
        "Clean Architecture",
        "Dependency Injection",
        "Repository Pattern",
        "CommunityToolkit.Mvvm",
        "Refit",
        "FluentValidation",
        "Serilog",
        "xUnit",
        "Moq",
        "Azure",
        "Linux",
        "Windows",
        "Android"
    ];

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
        profile.Summary = ExtractProfessionalTitle(lines);

        foreach (var skill in ExtractSkills(resumeText))
        {
            profile.Skills.Add(skill);
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

    private static IEnumerable<string> ExtractSkills(string resumeText)
    {
        foreach (var skill in KnownSkills)
        {
            if (resumeText.Contains(
                    skill,
                    StringComparison.OrdinalIgnoreCase))
            {
                yield return skill;
            }
        }
    }
}
