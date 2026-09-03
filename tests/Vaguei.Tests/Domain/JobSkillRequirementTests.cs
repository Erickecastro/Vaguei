using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class JobSkillRequirementTests
{
    [Fact]
    public void Constructor_CreatesRequirement()
    {
        var requirement = new JobSkillRequirement(
            "Gestão de projetos",
            JobSkillRequirementLevel.Required);

        Assert.Equal(
            "Gestão de projetos",
            requirement.Name);

        Assert.Equal(
            JobSkillRequirementLevel.Required,
            requirement.Level);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var requirement = new JobSkillRequirement(
            "  Excel  ",
            JobSkillRequirementLevel.Core);

        Assert.Equal(
            "Excel",
            requirement.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidName(
        string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new JobSkillRequirement(
                name!,
                JobSkillRequirementLevel.Mentioned));
    }
}
