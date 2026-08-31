using System.IO.Compression;
using System.Text;
using Vaguei.ResumeParser.Parsers;

namespace Vaguei.Tests.ResumeParser;

public sealed class OdtResumeParserTests
{
    [Fact]
    public void CanParse_WithOdtExtension_ReturnsTrue()
    {
        var parser = new OdtResumeParser();

        var result = parser.CanParse(".odt");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUppercaseOdtExtension_ReturnsTrue()
    {
        var parser = new OdtResumeParser();

        var result = parser.CanParse(".ODT");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUnsupportedExtension_ReturnsFalse()
    {
        var parser = new OdtResumeParser();

        var result = parser.CanParse(".docx");

        Assert.False(result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithValidOdt_ReturnsText()
    {
        var parser = new OdtResumeParser();

        await using var stream = CreateOdtStream(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content
                xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">

                <office:body>
                    <office:text>
                        <text:p>Pessoa Teste</text:p>
                        <text:p>Desenvolvedor Backend</text:p>
                        <text:p>C# e PostgreSQL</text:p>
                    </office:text>
                </office:body>
            </office:document-content>
            """);

        var result =
            await parser.ExtractTextAsync(stream);

        var expected = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "C# e PostgreSQL");

        Assert.Equal(
            expected,
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithFormattingElements_PreservesText()
    {
        var parser = new OdtResumeParser();

        await using var stream = CreateOdtStream(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content
                xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">

                <office:body>
                    <office:text>
                        <text:p>
                            C#<text:s/>ASP.NET Core<text:tab/>PostgreSQL
                        </text:p>
                    </office:text>
                </office:body>
            </office:document-content>
            """);

        var result =
            await parser.ExtractTextAsync(stream);

        Assert.Contains(
            "C# ASP.NET Core",
            result);

        Assert.Contains(
            "PostgreSQL",
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyParagraphs_RemovesEmptyLines()
    {
        var parser = new OdtResumeParser();

        await using var stream = CreateOdtStream(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content
                xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">

                <office:body>
                    <office:text>
                        <text:p>Pessoa Teste</text:p>
                        <text:p></text:p>
                        <text:p>Desenvolvedor .NET</text:p>
                    </office:text>
                </office:body>
            </office:document-content>
            """);

        var result =
            await parser.ExtractTextAsync(stream);

        var expected = string.Join(
            Environment.NewLine,
            "Pessoa Teste",
            "Desenvolvedor .NET");

        Assert.Equal(
            expected,
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithoutContentXml_ThrowsInvalidDataException()
    {
        var parser = new OdtResumeParser();

        await using var stream =
            new MemoryStream();

        using (var archive = new ZipArchive(
                   stream,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            var entry =
                archive.CreateEntry("arquivo.txt");

            await using var entryStream =
                entry.Open();

            await using var writer =
                new StreamWriter(
                    entryStream,
                    Encoding.UTF8);

            await writer.WriteAsync(
                "Arquivo sem content.xml");
        }

        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(
            async () =>
                await parser.ExtractTextAsync(stream));
    }

    private static MemoryStream CreateOdtStream(
        string contentXml)
    {
        var stream =
            new MemoryStream();

        using (var archive = new ZipArchive(
                   stream,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            var contentEntry =
                archive.CreateEntry("content.xml");

            using var entryStream =
                contentEntry.Open();

            using var writer =
                new StreamWriter(
                    entryStream,
                    new UTF8Encoding(
                        encoderShouldEmitUTF8Identifier: false));

            writer.Write(contentXml);
        }

        stream.Position = 0;

        return stream;
    }
}