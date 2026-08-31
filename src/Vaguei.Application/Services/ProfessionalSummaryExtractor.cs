namespace Vaguei.Application.Services;

public sealed class ProfessionalSummaryExtractor
{
    private static readonly string[] SectionTitles =
    [
        "RESUMO PROFISSIONAL",
        "PERFIL PROFISSIONAL",
        "SOBRE MIM",
        "SOBRE"
    ];

    private static readonly string[] SectionEndMarkers =
    [
        "EXPERIÊNCIA PROFISSIONAL",
        "EXPERIÊNCIAS PROFISSIONAIS",
        "EXPERIÊNCIA",
        "FORMAÇÃO ACADÊMICA",
        "FORMAÇÃO",
        "EDUCAÇÃO",
        "HABILIDADES",
        "COMPETÊNCIAS",
        "LINGUAGENS E TECNOLOGIAS",
        "TECNOLOGIAS",
        "PROJETOS",
        "PROJETOS PESSOAIS",
        "CURSOS",
        "CURSOS COMPLEMENTARES",
        "CERTIFICAÇÕES",
        "IDIOMAS"
    ];

    public string Extract(string resumeText)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return string.Empty;
        }

        var lines = resumeText
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var startIndex = FindSectionStart(lines);

        if (startIndex < 0)
        {
            return string.Empty;
        }

        var summaryLines = new List<string>();

        for (var index = startIndex + 1;
             index < lines.Length;
             index++)
        {
            var line = lines[index];

            if (IsSectionEnd(line))
            {
                break;
            }

            summaryLines.Add(line);
        }

        return string.Join(
            " ",
            summaryLines);
    }

    private static int FindSectionStart(
        string[] lines)
    {
        return Array.FindIndex(
            lines,
            line => SectionTitles.Any(
                title => string.Equals(
                    line,
                    title,
                    StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsSectionEnd(
        string line)
    {
        return SectionEndMarkers.Any(
            marker => string.Equals(
                line,
                marker,
                StringComparison.OrdinalIgnoreCase));
    }
}