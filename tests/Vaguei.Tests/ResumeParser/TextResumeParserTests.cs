using System.Text;
using Vaguei.ResumeParser.Parsers;

namespace Vaguei.Tests.ResumeParser;

public sealed class TextResumeParserTests
{
    [Fact]
    public void CanParse_WithTxtExtension_ReturnsTrue()
    {
        var parser = new TextResumeParser();

        var result = parser.CanParse(".txt");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUppercaseTxtExtension_ReturnsTrue()
    {
        var parser = new TextResumeParser();

        var result = parser.CanParse(".TXT");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUnsupportedExtension_ReturnsFalse()
    {
        var parser = new TextResumeParser();

        var result = parser.CanParse(".pdf");

        Assert.False(result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithUtf8Text_ReturnsNormalizedText()
    {
        var parser = new TextResumeParser();

        var content =
            "Ericke Castro\r\n\r\nDesenvolvedor .NET\r\nC# e PostgreSQL";

        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(content));

        var result = await parser.ExtractTextAsync(stream);

        var expected = string.Join(
            Environment.NewLine,
            "Ericke Castro",
            "Desenvolvedor .NET",
            "C# e PostgreSQL");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithWindows1252Text_ReturnsDecodedText()
    {
        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        var parser = new TextResumeParser();

        var encoding =
            Encoding.GetEncoding(1252);

        var content =
            "EXPERIÊNCIA PROFISSIONAL\r\n" +
            "Estagiário em Desenvolvimento de Sistemas\r\n" +
            "Segurança Pública";

        await using var stream = new MemoryStream(
            encoding.GetBytes(content));

        var result = await parser.ExtractTextAsync(stream);

        Assert.Contains(
            "EXPERIÊNCIA PROFISSIONAL",
            result);

        Assert.Contains(
            "Estagiário",
            result);

        Assert.Contains(
            "Segurança Pública",
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_RemovesEmptyLinesAndTrimsWhitespace()
    {
        var parser = new TextResumeParser();

        var content =
            "  Ericke Castro  \n\n   Desenvolvedor .NET   \n   ";

        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(content));

        var result = await parser.ExtractTextAsync(stream);

        var expected = string.Join(
            Environment.NewLine,
            "Ericke Castro",
            "Desenvolvedor .NET");

        Assert.Equal(expected, result);
    }
}