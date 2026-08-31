using System.Text;
using System.Text.RegularExpressions;
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

        for (var index = 0;
             index < rawLines.Count;
             index++)
        {
            var currentLine = rawLines[index];

            if (normalizedLines.Count == 0)
            {
                normalizedLines.Add(currentLine);
                continue;
            }

            var previousLine =
                normalizedLines[^1];

            var nextLine =
                index + 1 < rawLines.Count
                    ? rawLines[index + 1]
                    : null;

            if (ShouldJoinBulletContinuation(
                    previousLine,
                    currentLine,
                    nextLine))
            {
                normalizedLines[^1] =
                    $"{previousLine} {currentLine}";
            }
            else
            {
                normalizedLines.Add(currentLine);
            }
        }

        return string.Join(
            Environment.NewLine,
            normalizedLines);
    }

    private static bool ShouldJoinBulletContinuation(
        string previousLine,
        string currentLine,
        string? nextLine)
    {
        if (string.IsNullOrWhiteSpace(previousLine) ||
            string.IsNullOrWhiteSpace(currentLine))
        {
            return false;
        }

        if (!IsBulletLine(previousLine))
        {
            return false;
        }

        if (IsBulletLine(currentLine))
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

        // Se a próxima linha contém empresa + período,
        // a linha atual provavelmente é um cargo.
        if (nextLine is not null &&
            LooksLikeExperiencePeriod(nextLine))
        {
            return false;
        }

        // Se a próxima linha inicia outro bullet e a linha
        // atual não parece finalizar uma frase, ela pode ser
        // um título, como um projeto.
        if (nextLine is not null &&
            IsBulletLine(nextLine) &&
            !EndsWithSentencePunctuation(currentLine))
        {
            return false;
        }

        return true;
    }

    private static bool IsBulletLine(string line)
    {
        return line.StartsWith('·') ||
               line.StartsWith('•');
    }

    private static bool EndsWithSentencePunctuation(
        string line)
    {
        return line.EndsWith('.') ||
               line.EndsWith('!') ||
               line.EndsWith('?') ||
               line.EndsWith(';');
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
               Regex.IsMatch(
                   line,
                   @"\b\d{4}\b\s*[—–-]\s*(?:\b\d{4}\b|Atual)",
                   RegexOptions.IgnoreCase |
                   RegexOptions.CultureInvariant);
    }
}