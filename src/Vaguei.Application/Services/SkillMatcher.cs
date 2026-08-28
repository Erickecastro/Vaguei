using System.Text.RegularExpressions;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class SkillMatcher
{
    public bool ContainsSkill(
        string text,
        SkillDefinition skill)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (MatchesTerm(text, skill.Name))
        {
            return true;
        }

        return skill.Aliases.Any(alias =>
            MatchesTerm(text, alias));
    }

    private static bool MatchesTerm(
        string text,
        string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var escapedTerm = Regex.Escape(term);

        var pattern =
            $@"(?<![\p{{L}}\p{{N}}]){escapedTerm}(?![\p{{L}}\p{{N}}])";

        return Regex.IsMatch(
            text,
            pattern,
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);
    }
}
