using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;

namespace Vaguei.Tests.ResumeParser;

public sealed class ResumeParserServiceTests
{
    private readonly ResumeParserService _service = new(
    [
        new OdtResumeParser(),
        new DocxResumeParser(),
        new PdfResumeParser(),
        new TextResumeParser()
    ]);

    [Theory]
    [InlineData(".odt", typeof(OdtResumeParser))]
    [InlineData(".docx", typeof(DocxResumeParser))]
    [InlineData(".pdf", typeof(PdfResumeParser))]
    [InlineData(".txt", typeof(TextResumeParser))]
    public void GetParser_WithSupportedExtension_ReturnsCorrectParser(
        string extension,
        Type expectedType)
    {
        var parser = _service.GetParser(extension);

        Assert.IsType(expectedType, parser);
    }

    [Theory]
    [InlineData(".ODT")]
    [InlineData(".DOCX")]
    [InlineData(".PDF")]
    [InlineData(".TXT")]
    public void GetParser_WithUppercaseExtension_ReturnsParser(
        string extension)
    {
        var parser = _service.GetParser(extension);

        Assert.NotNull(parser);
    }

    [Fact]
    public void GetParser_WithUnsupportedExtension_ThrowsNotSupportedException()
    {
        var action = () =>
            _service.GetParser(".rtf");

        Assert.Throws<NotSupportedException>(action);
    }
}