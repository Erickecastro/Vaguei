using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobSearchTermGeneratorTests
{
    [Fact]
    public void Generate_UsesProfessionalTitle()
    {
        var profile = new CandidateProfile
        {
            ProfessionalTitle =
                "Desenvolvedor .NET"
        };

        var preferences =
            new JobSearchPreferences();

        var generator =
            new JobSearchTermGenerator();

        var terms =
            generator.Generate(
                profile,
                preferences);

        Assert.Contains(
            "Desenvolvedor .NET",
            terms);
    }

    [Fact]
    public void Generate_AddsDotNetRoleVariants()
    {
        var profile = new CandidateProfile
        {
            ProfessionalTitle =
                "Desenvolvedor .NET",

            Skills =
            [
                ".NET",
                "C#",
                "ASP.NET Core"
            ]
        };

        var preferences =
            new JobSearchPreferences();

        var generator =
            new JobSearchTermGenerator();

        var terms =
            generator.Generate(
                profile,
                preferences);

        Assert.Contains(
            ".NET Developer",
            terms);

        Assert.Contains(
            "C# Developer",
            terms);

        Assert.Contains(
            "Backend Developer",
            terms);

        Assert.Contains(
            "Software Engineer",
            terms);
    }

    [Fact]
    public void Generate_AddsRelevantSkillTerms()
    {
        var profile = new CandidateProfile
        {
            Skills =
            [
                "C#",
                ".NET",
                "ASP.NET Core"
            ]
        };

        var preferences =
            new JobSearchPreferences();

        var generator =
            new JobSearchTermGenerator();

        var terms =
            generator.Generate(
                profile,
                preferences);

        Assert.Contains(
            "C#",
            terms);

        Assert.Contains(
            ".NET",
            terms);

        Assert.Contains(
            "ASP.NET Core",
            terms);
    }

    [Fact]
    public void Generate_PrioritizesDesiredRoles()
    {
        var profile = new CandidateProfile
        {
            ProfessionalTitle =
                "Desenvolvedor .NET",

            Skills =
            [
                ".NET",
                "C#"
            ]
        };

        var preferences =
            new JobSearchPreferences();

        preferences.DesiredRoles.Add(
            "Frontend Developer");

        var generator =
            new JobSearchTermGenerator();

        var terms =
            generator.Generate(
                profile,
                preferences);

        Assert.Single(terms);

        Assert.Contains(
            "Frontend Developer",
            terms);

        Assert.DoesNotContain(
            ".NET Developer",
            terms);

        Assert.DoesNotContain(
            "Desenvolvedor .NET",
            terms);
    }

    [Fact]
    public void Generate_ExpandsInternshipSearchIntent()
    {
        var preferences = new JobSearchPreferences();
        preferences.DesiredRoles.Add("Estágio");

        var terms = new JobSearchTermGenerator().Generate(
            new CandidateProfile(),
            preferences);

        Assert.Contains("Estágio", terms);
        Assert.Contains("estagio", terms);
        Assert.Contains("estagiário", terms);
        Assert.Contains("estagiaria", terms);
        Assert.Contains("internship", terms);
        Assert.Contains("intern", terms);
    }

    [Fact]
    public void Generate_IgnoresEmptyProfessionalTitle()
    {
        var profile =
            new CandidateProfile();

        var preferences =
            new JobSearchPreferences();

        var generator =
            new JobSearchTermGenerator();

        var terms =
            generator.Generate(
                profile,
                preferences);

        Assert.Empty(terms);
    }

    [Fact]
    public void Generate_DoesNotUseLowSignalSkills()
    {
        var profile = new CandidateProfile
        {
            Skills =
            [
                "Git",
                "GitHub",
                "Swagger",
                "SOLID",
                "Docker"
            ]
        };

        var preferences =
            new JobSearchPreferences();

        var generator =
            new JobSearchTermGenerator();

        var terms =
            generator.Generate(
                profile,
                preferences);

        Assert.Empty(terms);
    }
}
