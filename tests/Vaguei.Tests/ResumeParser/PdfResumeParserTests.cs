using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Vaguei.ResumeParser.Parsers;

namespace Vaguei.Tests.ResumeParser;

public sealed class PdfResumeParserTests
{
    [Fact]
    public void CanParse_WithPdfExtension_ReturnsTrue()
    {
        var parser = new PdfResumeParser();

        var result = parser.CanParse(".pdf");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUppercasePdfExtension_ReturnsTrue()
    {
        var parser = new PdfResumeParser();

        var result = parser.CanParse(".PDF");

        Assert.True(result);
    }

    [Fact]
    public void CanParse_WithUnsupportedExtension_ReturnsFalse()
    {
        var parser = new PdfResumeParser();

        var result = parser.CanParse(".docx");

        Assert.False(result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithValidPdf_ReturnsText()
    {
        var parser = new PdfResumeParser();

        await using var stream = CreatePdfStream(
        [
            "Pessoa Teste",
            "Desenvolvedor Backend",
            "C# PostgreSQL ASP.NET Core"
        ]);

        var result =
            await parser.ExtractTextAsync(stream);

        Assert.Contains(
            "Pessoa Teste",
            result);

        Assert.Contains(
            "Desenvolvedor Backend",
            result);

        Assert.Contains(
            "C# PostgreSQL ASP.NET Core",
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithMultipleLines_PreservesReadingOrder()
    {
        var parser = new PdfResumeParser();

        await using var stream = CreatePdfStream(
        [
            "Pessoa Teste",
            "Desenvolvedor .NET",
            "EXPERIENCIA PROFISSIONAL",
            "Desenvolvedor Backend",
            "Empresa de Tecnologia | 2024 - Atual"
        ]);

        var result =
            await parser.ExtractTextAsync(stream);

        var lines = result
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        Assert.Equal(
            "Pessoa Teste",
            lines[0]);

        Assert.Equal(
            "Desenvolvedor .NET",
            lines[1]);

        Assert.Contains(
            "EXPERIENCIA PROFISSIONAL",
            lines);
    }

    [Fact]
    public async Task ExtractTextAsync_WithMultiplePages_ReturnsContentFromAllPages()
    {
        var parser = new PdfResumeParser();

        await using var stream = CreateMultiPagePdfStream();

        var result =
            await parser.ExtractTextAsync(stream);

        Assert.Contains(
            "Primeira pagina",
            result);

        Assert.Contains(
            "Segunda pagina",
            result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithInvalidPdf_ThrowsException()
    {
        var parser = new PdfResumeParser();

        await using var stream =
            new MemoryStream(
                "Isto nao e um PDF"u8.ToArray());

        await Assert.ThrowsAnyAsync<Exception>(
            async () =>
                await parser.ExtractTextAsync(stream));
    }

    private static MemoryStream CreatePdfStream(
        IReadOnlyList<string> lines)
    {
        var builder =
            new PdfDocumentBuilder();

        var font =
            builder.AddStandard14Font(
                Standard14Font.Helvetica);

        var page =
            builder.AddPage(
                PageSize.A4);

        int y = 780;

        foreach (var line in lines)
        {
            page.AddText(
                line,
                12,
                new PdfPoint(
                    50,
                    y),
                font);

            y -= 20;
        }

        var bytes =
            builder.Build();

        return new MemoryStream(
            bytes);
    }

    private static MemoryStream CreateMultiPagePdfStream()
    {
        var builder =
            new PdfDocumentBuilder();

        var font =
            builder.AddStandard14Font(
                Standard14Font.Helvetica);

        var firstPage =
            builder.AddPage(
                PageSize.A4);

        firstPage.AddText(
            "Primeira pagina",
            12,
            new PdfPoint(
                50,
                780),
            font);

        var secondPage =
            builder.AddPage(
                PageSize.A4);

        secondPage.AddText(
            "Segunda pagina",
            12,
            new PdfPoint(
                50,
                780),
            font);

        return new MemoryStream(
            builder.Build());
    }
}