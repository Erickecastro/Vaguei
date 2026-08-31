using System.Text.RegularExpressions;
using Vaguei.Domain.Entities;

namespace Vaguei.Application.Services;

public sealed class WorkExperienceExtractor
{
    private const string ExperienceSection =
        "EXPERIÊNCIA PROFISSIONAL";

    private static readonly string[] SectionEndMarkers =
    [
        "PROJETOS PESSOAIS",
        "FORMAÇÃO ACADÊMICA",
        "LINGUAGENS E TECNOLOGIAS",
        "CURSOS COMPLEMENTARES",
        "IDIOMAS"
    ];

    public IReadOnlyCollection<WorkExperience> Extract(string resumeText)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return [];
        }

        var lines = resumeText
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var sectionLines = ExtractExperienceSection(lines);

        if (sectionLines.Count == 0)
        {
            return [];
        }

        return ParseExperiences(sectionLines);
    }

    private static List<string> ExtractExperienceSection(
        string[] lines)
    {
        var result = new List<string>();

        var startIndex = Array.FindIndex(
            lines,
            line => string.Equals(
                line,
                ExperienceSection,
                StringComparison.OrdinalIgnoreCase));

        if (startIndex < 0)
        {
            return result;
        }

        for (var index = startIndex + 1;
             index < lines.Length;
             index++)
        {
            var line = lines[index];

            if (SectionEndMarkers.Any(marker =>
                    string.Equals(
                        line,
                        marker,
                        StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            result.Add(line);
        }

        return result;
    }

    private static IReadOnlyCollection<WorkExperience> ParseExperiences(
        List<string> lines)
    {
        var experiences = new List<WorkExperience>();

        var index = 0;

        while (index < lines.Count)
        {
            var position = lines[index];

            if (index + 1 >= lines.Count)
            {
                break;
            }

            var companyAndPeriod = lines[index + 1];

            if (!LooksLikeCompanyAndPeriod(companyAndPeriod))
            {
                index++;
                continue;
            }

            var experience = CreateExperience(
                position,
                companyAndPeriod);

            index += 2;

            var descriptionLines = new List<string>();

            while (index < lines.Count)
            {
                if (index + 1 < lines.Count &&
                    LooksLikeCompanyAndPeriod(lines[index + 1]))
                {
                    break;
                }

                descriptionLines.Add(
                    CleanDescriptionLine(lines[index]));

                index++;
            }

            experience.Description = string.Join(
                Environment.NewLine,
                descriptionLines
                    .Where(line =>
                        !string.IsNullOrWhiteSpace(line)));

            experiences.Add(experience);
        }

        return experiences;
    }

    private static bool LooksLikeCompanyAndPeriod(string line)
    {
        if (!line.Contains('|'))
        {
            return false;
        }

        return Regex.IsMatch(
            line,
            @"\b\d{4}\b\s*[—–-]\s*(?:\b\d{4}\b|Atual)",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);
    }

    private static WorkExperience CreateExperience(
        string position,
        string companyAndPeriod)
    {
        var parts = companyAndPeriod.Split(
            '|',
            2,
            StringSplitOptions.TrimEntries);

        var company = parts[0];

        var period = parts.Length > 1
            ? parts[1]
            : string.Empty;

        var periodMatch = Regex.Match(
            period,
            @"(?<start>\d{4})\s*[—–-]\s*(?<end>\d{4}|Atual)",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

        int? startYear = null;
        int? endYear = null;
        var isCurrent = false;

        if (periodMatch.Success)
        {
            if (int.TryParse(
                    periodMatch.Groups["start"].Value,
                    out var parsedStartYear))
            {
                startYear = parsedStartYear;
            }

            var endValue =
                periodMatch.Groups["end"].Value;

            if (string.Equals(
                    endValue,
                    "Atual",
                    StringComparison.OrdinalIgnoreCase))
            {
                isCurrent = true;
            }
            else if (int.TryParse(
                         endValue,
                         out var parsedEndYear))
            {
                endYear = parsedEndYear;
            }
        }

        return new WorkExperience
        {
            Position = position,
            Company = company,
            StartYear = startYear,
            EndYear = endYear,
            IsCurrent = isCurrent
        };
    }

    private static string CleanDescriptionLine(string line)
    {
        var cleanedLine = line
            .Trim()
            .TrimStart('·', '•')
            .Trim();

        cleanedLine = cleanedLine
            .Replace(" · ", Environment.NewLine)
            .Replace(" • ", Environment.NewLine);

        return cleanedLine;
    }
}