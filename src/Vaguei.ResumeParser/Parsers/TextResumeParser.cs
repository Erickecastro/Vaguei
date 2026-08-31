using System.Text;
using Vaguei.Application.Interfaces;

namespace Vaguei.ResumeParser.Parsers;

public sealed class TextResumeParser : IResumeParser
{
    public bool CanParse(string extension)
    {
        return string.Equals(
            extension,
            ".txt",
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

        var bytes = memoryStream.ToArray();

        var text = DecodeText(bytes);

        return NormalizeText(text);
    }

    private static string DecodeText(byte[] bytes)
    {
        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        var utf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        try
        {
            return utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            var windows1252 =
                Encoding.GetEncoding(1252);

            return windows1252.GetString(bytes);
        }
    }

    private static string NormalizeText(string text)
    {
        var lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line =>
                !string.IsNullOrWhiteSpace(line));

        return string.Join(
            Environment.NewLine,
            lines);
    }
}