using System.IO.Compression;
using System.Text;
using Vaguei.ResumeParser.Parsers;

namespace Vaguei.Tests.ResumeParser;

public sealed class DocxResumeParserTests
{
    [Fact]
    public void CanParse_WithDocxExtension_ReturnsTrue()
    {
        var parser = new DocxResumeParser();

        var result = parser.CanParse(".docx");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUppercaseDocxExtension_ReturnsTrue()
    {
        var parser = new DocxResumeParser();

        var result = parser.CanParse(".DOCX");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUnsupportedExtension_ReturnsFalse()
    {
        var parser = new DocxResumeParser();

        var result = parser.CanParse(".pdf");

        Assert.False(result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithValidDocx_ReturnsText()
    {
        var parser = new DocxResumeParser();

        await using var stream = CreateDocxStream(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document
                xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">

                <w:body>
                    <w:p>
                        <w:r>
                            <w:t>Pessoa Teste</w:t>
                        </w:r>
                    </w:p>

                    <w:p>
                        <w:r>
                            <w:t>Desenvolvedor Backend</w:t>
                        </w:r>
                    </w:p>

                    <w:p>
                        <w:r>
                            <w:t>C# e PostgreSQL</w:t>
                        </w:r>
                    </w:p>
                </w:body>
            </w:document>
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
    public async Task ExtractTextAsync_WithMultipleRuns_JoinsText()
    {
        var parser = new DocxResumeParser();

        await using var stream = CreateDocxStream(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document
                xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">

                <w:body>
                    <w:p>
                        <w:r>
                            <w:t>Desenvolvedor </w:t>
                        </w:r>

                        <w:r>
                            <w:t>.NET</w:t>
                        </w:r>
                    </w:p>
                </w:body>
            </w:document>
            """);

        var result =
            await parser.ExtractTextAsync(stream);

        Assert.Equal(
            "Desenvolvedor .NET",
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithTabAndLineBreak_PreservesStructure()
    {
        var parser = new DocxResumeParser();

        await using var stream = CreateDocxStream(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document
                xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">

                <w:body>
                    <w:p>
                        <w:r>
                            <w:t>C#</w:t>
                            <w:tab/>
                            <w:t>PostgreSQL</w:t>
                            <w:br/>
                            <w:t>ASP.NET Core</w:t>
                        </w:r>
                    </w:p>
                </w:body>
            </w:document>
            """);

        var result =
            await parser.ExtractTextAsync(stream);

        Assert.Contains(
            "C# PostgreSQL",
            result);

        Assert.Contains(
            "ASP.NET Core",
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyParagraphs_RemovesEmptyLines()
    {
        var parser = new DocxResumeParser();

        await using var stream = CreateDocxStream(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document
                xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">

                <w:body>
                    <w:p>
                        <w:r>
                            <w:t>Pessoa Teste</w:t>
                        </w:r>
                    </w:p>

                    <w:p />

                    <w:p>
                        <w:r>
                            <w:t>Desenvolvedor .NET</w:t>
                        </w:r>
                    </w:p>
                </w:body>
            </w:document>
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
    public async Task ExtractTextAsync_WithoutDocumentXml_ThrowsInvalidDataException()
    {
        var parser = new DocxResumeParser();

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
                "Arquivo sem word/document.xml");
        }

        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(
            async () =>
                await parser.ExtractTextAsync(stream));
    }

    private static MemoryStream CreateDocxStream(
        string documentXml)
    {
        var stream =
            new MemoryStream();

        using (var archive = new ZipArchive(
                   stream,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            var documentEntry =
                archive.CreateEntry(
                    "word/document.xml");

            using var entryStream =
                documentEntry.Open();

            using var writer =
                new StreamWriter(
                    entryStream,
                    new UTF8Encoding(
                        encoderShouldEmitUTF8Identifier: false));

            writer.Write(documentXml);
        }

        stream.Position = 0;

        return stream;
    }
}