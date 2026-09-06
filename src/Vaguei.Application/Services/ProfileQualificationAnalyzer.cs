using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class ProfileQualificationAnalyzer
{
    private static readonly (string Name, string[] Terms)[] CertificationCatalog =
    [
        ("PMP", ["pmp", "project management professional"]),
        ("Scrum", ["certified scrum master", "csm", "psm i", "psm ii"]),
        ("AWS", ["aws certified", "certificação aws", "certificacao aws"]),
        ("Azure", ["microsoft certified", "azure fundamentals", "azure administrator"]),
        ("Google Cloud", ["google cloud certified", "professional cloud architect"]),
        ("ITIL", ["itil foundation", "certificação itil", "certificacao itil"]),
        ("CPA-10", ["cpa-10", "cpa 10"]),
        ("CPA-20", ["cpa-20", "cpa 20"]),
        ("CRC", ["registro no crc", "crc ativo"]),
        ("NR-10", ["nr-10", "nr 10"]),
        ("NR-35", ["nr-35", "nr 35"])
    ];

    private static readonly (string Name, string[] Terms)[] LanguageCatalog =
    [
        ("Inglês", ["inglês", "ingles", "english"]),
        ("Espanhol", ["espanhol", "español", "spanish"]),
        ("Francês", ["francês", "frances", "français", "french"])
    ];

    public EducationLevel ExtractEducationLevel(string text)
    {
        var normalized = Normalize(text);

        if (ContainsAny(normalized, "doutorado", "doctorate", "phd"))
            return EducationLevel.Doctorate;
        if (ContainsAny(normalized, "mestrado", "master degree", "master's degree", "masters degree"))
            return EducationLevel.Masters;
        if (ContainsAny(normalized, "pos-graduacao", "pos graduacao", "mba", "postgraduate"))
            return EducationLevel.Graduate;
        if (ContainsAny(normalized, "bacharelado", "graduacao", "ensino superior", "bachelor", "college degree", "university degree"))
            return EducationLevel.Undergraduate;
        if (ContainsAny(normalized, "curso tecnico", "ensino tecnico", "technical degree", "technical diploma"))
            return EducationLevel.Technical;
        if (ContainsAny(normalized, "ensino medio", "high school"))
            return EducationLevel.Secondary;

        return EducationLevel.Unknown;
    }

    public IReadOnlyCollection<string> ExtractCertifications(string text) =>
        CertificationCatalog
            .Where(item => item.Terms.Any(term => ContainsTerm(text, term)))
            .Select(item => item.Name)
            .ToArray();

    public IReadOnlyCollection<string> ExtractRequiredCertifications(string text) =>
        CertificationCatalog
            .Where(item => item.Terms.Any(term =>
                IsExplicitRequirement(text, term)))
            .Select(item => item.Name)
            .ToArray();

    public IReadOnlyCollection<CandidateLanguage> ExtractLanguages(string text)
    {
        var results = new List<CandidateLanguage>();

        foreach (var language in LanguageCatalog)
        {
            var proficiency = language.Terms
                .Select(term => ExtractLanguageProficiency(text, term))
                .Where(level => level != LanguageProficiency.Unknown)
                .DefaultIfEmpty(LanguageProficiency.Unknown)
                .Max();

            if (proficiency != LanguageProficiency.Unknown ||
                language.Terms.Any(term => ContainsTerm(text, term)))
            {
                results.Add(new CandidateLanguage(language.Name, proficiency));
            }
        }

        return results;
    }

    private static LanguageProficiency ExtractLanguageProficiency(
        string text,
        string language)
    {
        var normalized = Normalize(text);
        var normalizedLanguage = Normalize(language);
        var pattern = $@"(?:{Regex.Escape(normalizedLanguage)}.{{0,35}}(?<level>nativo|native|fluente|fluent|avancado|advanced|intermediario|intermediate|basico|basic)|(?<level>nativo|native|fluente|fluent|avancado|advanced|intermediario|intermediate|basico|basic).{{0,35}}{Regex.Escape(normalizedLanguage)})";
        var match = Regex.Match(normalized, pattern, RegexOptions.CultureInvariant);

        return match.Groups["level"].Value switch
        {
            "nativo" or "native" => LanguageProficiency.Native,
            "fluente" or "fluent" => LanguageProficiency.Fluent,
            "avancado" or "advanced" => LanguageProficiency.Advanced,
            "intermediario" or "intermediate" => LanguageProficiency.Intermediate,
            "basico" or "basic" => LanguageProficiency.Basic,
            _ => LanguageProficiency.Unknown
        };
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => ContainsTerm(text, term));

    private static bool ContainsTerm(string text, string term)
    {
        var normalizedText = Normalize(text);
        var normalizedTerm = Normalize(term);
        return Regex.IsMatch(
            normalizedText,
            $@"(?<![a-z0-9]){Regex.Escape(normalizedTerm)}(?![a-z0-9])",
            RegexOptions.CultureInvariant);
    }

    private static bool IsExplicitRequirement(string text, string term)
    {
        var normalizedText = Normalize(text);
        var normalizedTerm = Regex.Escape(Normalize(term));
        const string marker =
            @"obrigatorio|obrigatoria|necessario|necessaria|exigido|exigida|required|mandatory|must have";

        return Regex.IsMatch(
            normalizedText,
            $@"(?:{normalizedTerm}.{{0,50}}(?:{marker})|(?:{marker}).{{0,50}}{normalizedTerm})",
            RegexOptions.CultureInvariant);
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
