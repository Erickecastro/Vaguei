using Vaguei.Application.Services;
using Vaguei.Domain.Enums;

namespace Vaguei.Tests.Application;

public sealed class ProfileQualificationAnalyzerTests
{
    private readonly ProfileQualificationAnalyzer _analyzer = new();

    [Theory]
    [InlineData("Ensino médio completo", EducationLevel.Secondary)]
    [InlineData("Curso técnico em eletrônica", EducationLevel.Technical)]
    [InlineData("Bacharelado em Administração", EducationLevel.Undergraduate)]
    [InlineData("MBA em Gestão de Projetos", EducationLevel.Graduate)]
    [InlineData("Mestrado em Engenharia", EducationLevel.Masters)]
    public void ExtractEducationLevel_UsesHighestExplicitLevel(
        string text,
        EducationLevel expected)
    {
        Assert.Equal(expected, _analyzer.ExtractEducationLevel(text));
    }

    [Fact]
    public void ExtractCertifications_RecognizesControlledCatalog()
    {
        var certifications = _analyzer.ExtractCertifications(
            "Certificações: AWS Certified e ITIL Foundation.");

        Assert.Contains("AWS", certifications);
        Assert.Contains("ITIL", certifications);
    }

    [Fact]
    public void ExtractRequiredCertifications_UsesAliasesAndRequirementContext()
    {
        var certifications = _analyzer.ExtractRequiredCertifications(
            "CSM obrigatório para esta posição. ITIL será um diferencial.");

        Assert.Contains("Scrum", certifications);
        Assert.DoesNotContain("ITIL", certifications);
    }

    [Theory]
    [InlineData("Inglês fluente", "Inglês", LanguageProficiency.Fluent)]
    [InlineData("Advanced English", "Inglês", LanguageProficiency.Advanced)]
    [InlineData("Espanhol intermediário", "Espanhol", LanguageProficiency.Intermediate)]
    public void ExtractLanguages_RecognizesBilingualProficiency(
        string text,
        string language,
        LanguageProficiency expected)
    {
        var result = Assert.Single(
            _analyzer.ExtractLanguages(text),
            item => item.Name == language);

        Assert.Equal(expected, result.Proficiency);
    }
}
