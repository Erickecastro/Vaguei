using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Vaguei.Application.Services;

public sealed class ProfessionalRoleNormalizer
{
    private static readonly IReadOnlyDictionary<string, string>
        PhraseAliases =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["engenheiro de software"] =
                    "software developer",

                ["engenheira de software"] =
                    "software developer",

                ["software engineer"] =
                    "software developer",

                ["desenvolvedor de software"] =
                    "software developer",

                ["desenvolvedora de software"] =
                    "software developer",

                ["full stack"] =
                    "fullstack",

                ["full-stack"] =
                    "fullstack"
            };

    private static readonly IReadOnlyDictionary<string, string>
        TokenAliases =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["desenvolvedor"] =
                    "developer",

                ["desenvolvedora"] =
                    "developer",

                ["analista"] =
                    "analyst",

                ["assistente"] =
                    "assistant",

                ["administrativo"] =
                    "administrative",

                ["administrativa"] =
                    "administrative",

                ["contador"] =
                    "accountant",

                ["contadora"] =
                    "accountant",

                ["engenheiro"] =
                    "engineer",

                ["engenheira"] =
                    "engineer",

                ["enfermeiro"] =
                    "nurse",

                ["enfermeira"] =
                    "nurse",

                ["vendedor"] =
                    "sales",

                ["vendedora"] =
                    "sales",

                ["dados"] =
                    "data",

                [".net"] =
                    "dotnet"
            };

    private static readonly HashSet<string>
        IgnoredTerms =
        [
            "a",
            "as",
            "and",
            "da",
            "das",
            "de",
            "do",
            "dos",
            "e",
            "em",
            "of",
            "para",
            "the",

            "junior",
            "jr",
            "senior",
            "sr",
            "pleno",
            "mid",
            "trainee",
            "estagiario",
            "estagiaria",
            "intern"
        ];

    public IReadOnlyCollection<string> Normalize(
        string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return [];
        }

        var normalizedText =
            RemoveDiacritics(
                role);

        foreach (var alias in PhraseAliases)
        {
            normalizedText =
                ReplacePhrase(
                    normalizedText,
                    alias.Key,
                    alias.Value);
        }

        var terms =
            Regex.Matches(
                    normalizedText,
                    @"[\p{L}\p{N}+#.]+")
                .Select(
                    match =>
                        match.Value)
                .Where(
                    term =>
                        !IgnoredTerms.Contains(
                            term))
                .Select(
                    NormalizeToken)
                .Where(
                    term =>
                        !string.IsNullOrWhiteSpace(
                            term))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return terms;
    }

    private static string NormalizeToken(
        string token)
    {
        if (TokenAliases.TryGetValue(
                token,
                out var alias))
        {
            return alias;
        }

        return token;
    }

    private static string ReplacePhrase(
        string text,
        string phrase,
        string replacement)
    {
        var pattern =
            $@"(?<![\p{{L}}\p{{N}}])" +
            Regex.Escape(phrase) +
            @"(?![\p{L}\p{N}])";

        return Regex.Replace(
            text,
            pattern,
            replacement,
            RegexOptions.IgnoreCase);
    }

    private static string RemoveDiacritics(
        string text)
    {
        var decomposed =
            text
                .Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in decomposed)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(
                    character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC);
    }
}