using System.IO.Compression;
using System.Xml.Linq;
using Vaguei.Application.Interfaces;

namespace Vaguei.ResumeParser.Parsers;

public sealed class OdtResumeParser : IResumeParser
{
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
            LoadOptions.None,
            cancellationToken);

        XNamespace textNamespace =
            "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

        var paragraphs = document
            .Descendants()
            .Where(element =>
                element.Name == textNamespace + "p" ||
                element.Name == textNamespace + "h")
            .Select(element => element.Value.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(
            Environment.NewLine,
            paragraphs);
    }
}
