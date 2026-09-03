using System.Text.RegularExpressions;

namespace Vaguei.Application.Services;

public sealed partial class ResumeTextSanitizer
{
    public string Sanitize(
        string resumeText)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return string.Empty;
        }

        var sanitized = EmailPattern().Replace(
            resumeText,
            string.Empty);

        sanitized = UrlPattern().Replace(
            sanitized,
            string.Empty);

        sanitized = PhonePattern().Replace(
            sanitized,
            string.Empty);

        var lines = sanitized
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(RemoveEmptyContactLabel)
            .Where(line =>
                !string.IsNullOrWhiteSpace(line));

        return string.Join(
            Environment.NewLine,
            lines);
    }

    private static string RemoveEmptyContactLabel(
        string line)
    {
        return EmptyContactLabelPattern().IsMatch(line)
            ? string.Empty
            : line;
    }

    [GeneratedRegex(
        @"\b[\p{L}\p{N}.%_+\-]+@[\p{L}\p{N}.\-]+\.[\p{L}]{2,}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(
        @"(?:https?://|www\.)\S+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(
        @"(?<!\d)(?:\+?\d{1,3}[\s.\-]?)?(?:\(?\d{2,3}\)?[\s.\-]?)?\d{4,5}[\s.\-]?\d{4}(?!\d)",
        RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(
        @"^(?:e-?mail|email|telefone|tel|celular|phone|mobile)\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmptyContactLabelPattern();
}
