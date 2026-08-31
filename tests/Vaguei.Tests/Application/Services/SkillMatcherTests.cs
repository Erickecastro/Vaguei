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
}