using Vaguei.Application.Services;

namespace Vaguei.Tests.Application;

public sealed class ResumeTextSanitizerTests
{
    private readonly ResumeTextSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_RemovesContactInformation()
    {
        const string resume =
            """
            Pessoa Teste
            pessoa@example.com
            +55 (92) 98105-1875
            https://linkedin.com/in/pessoa
            Analista Financeiro
            """;

        var result = _sanitizer.Sanitize(resume);

        Assert.DoesNotContain("@", result);
        Assert.DoesNotContain("98105", result);
        Assert.DoesNotContain("linkedin.com", result);
        Assert.Contains("Pessoa Teste", result);
        Assert.Contains("Analista Financeiro", result);
    }

    [Fact]
    public void Sanitize_RemovesEmptyContactLabels()
    {
        const string resume =
            """
            E-mail: pessoa@example.com
            Telefone: +55 (92) 98105-1875
            Experiência profissional
            """;

        var result = _sanitizer.Sanitize(resume);

        Assert.DoesNotContain("E-mail", result);
        Assert.DoesNotContain("Telefone", result);
        Assert.Equal(
            "Experiência profissional",
            result);
    }

    [Fact]
    public void Sanitize_PreservesYearsAndProfessionalContent()
    {
        const string resume =
            """
            Empresa Exemplo | 2022 — 2026
            Gestão de projetos e atendimento ao cliente.
            """;

        var result = _sanitizer.Sanitize(resume);

        Assert.Contains("2022 — 2026", result);
        Assert.Contains("Gestão de projetos", result);
    }
}
