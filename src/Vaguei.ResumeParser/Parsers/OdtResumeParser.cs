using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Vaguei.Application.Interfaces;

namespace Vaguei.ResumeParser.Parsers;

public sealed class OdtResumeParser : IResumeParser
{
    private static readonly XNamespace TextNamespace =
        "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    public bool CanParse(string extension)
    {
        return string.Equals(
            extension,
            ".odt",
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

        var contentEntry = archive.GetEntry("content.xml");

        if (contentEntry is null)
        {
            throw new InvalidDataException(
                "O arquivo ODT não contém content.xml.");
        }

        await using var contentStream = contentEntry.Open();

        var document = await XDocument.LoadAsync(
            contentStream,
            LoadOptions.PreserveWhitespace,
            cancellationToken);

        var bodyText = document
            .Descendants()
            .FirstOrDefault(element =>
                element.Name.LocalName == "text");

        if (bodyText is null)
        {
            throw new InvalidDataException(
                "Não foi possível localizar o conteúdo textual do arquivo ODT.");
        }

        var builder = new StringBuilder();

        foreach (var node in bodyText.Nodes())
        {
            AppendNodeText(node, builder);
        }

        return NormalizeText(builder.ToString());
    }

    private static void AppendNodeText(
        XNode node,
        StringBuilder builder)
    {
        switch (node)
        {
            case XText textNode:
                builder.Append(textNode.Value);
                break;

            case XElement element:
                AppendElement(element, builder);
                break;
        }
    }

    private static void AppendElement(
        XElement element,
        StringBuilder builder)
    {
        if (element.Name == TextNamespace + "tab")
        {
            builder.Append('\t');
            return;
        }

        if (element.Name == TextNamespace + "line-break")
        {
            builder.AppendLine();
            return;
        }

        if (element.Name == TextNamespace + "s")
        {
            var countAttribute = element.Attribute(TextNamespace + "c");

            var count = 1;

            if (countAttribute is not null &&
                int.TryParse(countAttribute.Value, out var parsedCount))
            {
                count = parsedCount;
            }

            builder.Append(' ', count);
            return;
        }

        foreach (var node in element.Nodes())
        {
            AppendNodeText(node, builder);
        }

        if (element.Name == TextNamespace + "p" ||
            element.Name == TextNamespace + "h" ||
            element.Name == TextNamespace + "list-item")
        {
            builder.AppendLine();
        }
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
