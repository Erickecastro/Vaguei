using Vaguei.Application.Services;

namespace Vaguei.Tests.Application;

public sealed class ProfessionalRoleNormalizerTests
{
    private readonly ProfessionalRoleNormalizer
        _normalizer =
            new();

    [Fact]
    public void Normalize_ConvertsPortugueseDeveloper()
    {
        var result =
            _normalizer.Normalize(
                "Desenvolvedor .NET");

        Assert.Contains(
            "developer",
            result);

        Assert.Contains(
            "dotnet",
            result);
    }

    [Fact]
    public void Normalize_ConvertsSoftwareEngineer()
    {
        var result =
            _normalizer.Normalize(
                "Software Engineer");

        Assert.Contains(
            "software",
            result);

        Assert.Contains(
            "developer",
            result);

        Assert.DoesNotContain(
            "engineer",
            result);
    }

    [Fact]
    public void Normalize_ConvertsPortugueseSoftwareEngineer()
    {
        var result =
            _normalizer.Normalize(
                "Engenheiro de Software");

        Assert.Contains(
            "software",
            result);

        Assert.Contains(
            "developer",
            result);
    }

    [Fact]
    public void Normalize_RemovesSeniorityTerms()
    {
        var result =
            _normalizer.Normalize(
                "Senior Software Engineer");

        Assert.DoesNotContain(
            "senior",
            result);

        Assert.Contains(
            "software",
            result);

        Assert.Contains(
            "developer",
            result);
    }

    [Fact]
    public void Normalize_NormalizesAdministrativeRole()
    {
        var result =
            _normalizer.Normalize(
                "Assistente Administrativo");

        Assert.Contains(
            "assistant",
            result);

        Assert.Contains(
            "administrative",
            result);
    }

    [Fact]
    public void Normalize_NormalizesAccents()
    {
        var result =
            _normalizer.Normalize(
                "Analista de Dados Júnior");

        Assert.Contains(
            "analyst",
            result);

        Assert.Contains(
            "data",
            result);

        Assert.DoesNotContain(
            "junior",
            result);
    }

    [Fact]
    public void Normalize_PreservesUnknownTerms()
    {
        var result =
            _normalizer.Normalize(
                "Geotechnical Engineer");

        Assert.Contains(
            "geotechnical",
            result);

        Assert.Contains(
            "engineer",
            result);
    }
}