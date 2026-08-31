using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Vaguei.Application.Interfaces;

namespace Vaguei.ResumeParser.Parsers;

public sealed class DocxResumeParser : IResumeParser
{
    private static readonly XNamespace WordNamespace =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public bool CanParse(string extension)
    {
        return string.Equals(
            extension,
            ".docx",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(
            fileStream,
            ZipArchiveMode.Read,
            leaveOpen: true);

        var documentEntry = archive.GetEntry("word/document.xml");

        if (documentEntry is null)
        {
            throw new InvalidDataException(
                "O arquivo DOCX não contém word/document.xml.");
        }

        await using var documentStream = documentEntry.Open();

        var document = await XDocument.LoadAsync(
            documentStream,
            LoadOptions.PreserveWhitespace,
            cancellationToken);

        var builder = new StringBuilder();

        foreach (var paragraph in document
                     .Descendants(WordNamespace + "p"))
        {
            foreach (var node in paragraph.Descendants())
            {
                if (node.Name == WordNamespace + "t")
                {
                    builder.Append(node.Value);
                }
                else if (node.Name == WordNamespace + "tab")
                {
                    builder.Append(' ');
                }
                else if (node.Name == WordNamespace + "br")
                {
                    builder.AppendLine();
                }
            }

            builder.AppendLine();
        }

        return NormalizeText(builder.ToString());
    }

    private static string NormalizeText(string text)
    {
        var lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(
            Environment.NewLine,
            lines);
    }
}