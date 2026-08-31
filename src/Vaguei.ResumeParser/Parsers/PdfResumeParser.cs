using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Vaguei.Application.Interfaces;

namespace Vaguei.ResumeParser.Parsers;

public sealed class PdfResumeParser : IResumeParser
{
    public bool CanParse(string extension)
    {
        return string.Equals(
            extension,
            ".pdf",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(
            memoryStream,
            cancellationToken);

        memoryStream.Position = 0;

        using var document = PdfDocument.Open(
            memoryStream.ToArray());

        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageText =
                ContentOrderTextExtractor.GetText(page);

            builder.AppendLine(pageText);
        }

        return NormalizeText(
            builder.ToString());
    }

        private static string NormalizeText(string text)
    {
        var rawLines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var normalizedLines = new List<string>();

        foreach (var line in rawLines)
        {
            if (normalizedLines.Count == 0)
            {
                normalizedLines.Add(line);
                continue;
            }

            var previousLine =
                normalizedLines[^1];

            if (ShouldJoinLines(
                    previousLine,
                    line))
            {
                normalizedLines[^1] =
                    $"{previousLine} {line}";
            }
            else
            {
                normalizedLines.Add(line);
            }
        }

        return string.Join(
            Environment.NewLine,
            normalizedLines);
    }

    private static bool ShouldJoinLines(
        string previousLine,
        string currentLine)
    {
        if (string.IsNullOrWhiteSpace(previousLine) ||
            string.IsNullOrWhiteSpace(currentLine))
        {
            return false;
        }

        if (currentLine.StartsWith('·') ||
            currentLine.StartsWith('•'))
        {
            return false;
        }

        if (LooksLikeSectionTitle(currentLine))
        {
            return false;
        }

        if (LooksLikeExperiencePeriod(currentLine))
        {
            return false;
        }

        if (LooksLikeProfileHeader(currentLine))
        {
            return false;
        }

        if (previousLine.EndsWith('.') ||
            previousLine.EndsWith(':') ||
            previousLine.EndsWith(';'))
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeSectionTitle(
        string line)
    {
        var sectionTitles = new[]
        {
            "EXPERIÊNCIA PROFISSIONAL",
            "PROJETOS PESSOAIS",
            "LINGUAGENS E TECNOLOGIAS",
            "FORMAÇÃO ACADÊMICA",
            "CURSOS COMPLEMENTARES",
            "IDIOMAS"
        };

        return sectionTitles.Any(section =>
            string.Equals(
                line,
                section,
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeExperiencePeriod(
        string line)
    {
        return line.Contains('|') &&
               System.Text.RegularExpressions.Regex.IsMatch(
                   line,
                   @"\b\d{4}\b\s*[—–-]\s*(?:\b\d{4}\b|Atual)",
                   System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeProfileHeader(
        string line)
    {
        return line.Contains('@') ||
               line.StartsWith('+') ||
               line.StartsWith("http",
                   StringComparison.OrdinalIgnoreCase);
    }
}