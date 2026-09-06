using Vaguei.Application.Services;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application.Services;

public sealed class SkillMatcherTests
{
    private readonly SkillMatcher _matcher = new();

    [Fact]
    public void ContainsSkill_ShouldFindExactSkill()
    {
        var skill = new SkillDefinition
        {
            Name = "Git",
            Category = SkillCategory.Tool
        };

        var result = _matcher.ContainsSkill(
            "Versionamento de código com Git e GitHub.",
            skill);

        Assert.True(result);
    }

    [Fact]
    public void ContainsSkill_ShouldFindAlias()
    {
        var skill = new SkillDefinition
        {
            Name = "PostgreSQL",
            Category = SkillCategory.Database,
            Aliases = ["postgres"]
        };

        var result = _matcher.ContainsSkill(
            "Experiência com Postgres.",
            skill);

        Assert.True(result);
    }

    [Fact]
    public void ContainsSkill_ShouldIgnoreCase()
    {
        var skill = new SkillDefinition
        {
            Name = "Docker",
            Category = SkillCategory.DevOps
        };

        var result = _matcher.ContainsSkill(
            "Experiência utilizando DOCKER.",
            skill);

        Assert.True(result);
    }

    [Fact]
    public void ContainsSkill_ShouldNotMatchInsideAnotherWord()
    {
        var skill = new SkillDefinition
        {
            Name = "Git",
            Category = SkillCategory.Tool
        };

        var result = _matcher.ContainsSkill(
            "Digitalização do inventário patrimonial.",
            skill);

        Assert.False(result);
    }

    [Fact]
    public void ContainsSkill_ShouldReturnFalseForEmptyText()
    {
        var skill = new SkillDefinition
        {
            Name = "C#",
            Category = SkillCategory.Language
        };

        var result = _matcher.ContainsSkill(
            string.Empty,
            skill);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Fluent English is required.", "Inglês", "english")]
    [InlineData("Espanhol avançado.", "Espanhol", "spanish")]
    [InlineData("Experiência com Microsoft Excel.", "Excel", "Microsoft Excel")]
    [InlineData("Conhecimento em contas a pagar.", "Contas a Pagar", "accounts payable")]
    public void ContainsSkill_ShouldRecognizeNewDomainAliases(
        string text,
        string name,
        string alias)
    {
        var skill = new SkillDefinition
        {
            Name = name,
            Category = SkillCategory.SpokenLanguage,
            Aliases = [alias]
        };

        Assert.True(_matcher.ContainsSkill(text, skill));
    }
}
