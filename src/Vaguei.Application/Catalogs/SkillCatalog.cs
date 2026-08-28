using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Catalogs;

public static class SkillCatalog
{
    public static IReadOnlyCollection<SkillDefinition> Skills { get; } =
    [
        new()
        {
            Name = "C#",
            Category = SkillCategory.Language,
            Aliases = ["csharp"]
        },

        new()
        {
            Name = ".NET",
            Category = SkillCategory.Backend,
            Aliases = ["dotnet"]
        },

        new()
        {
            Name = "ASP.NET Core",
            Category = SkillCategory.Backend,
            Aliases = ["asp net core"]
        },

        new()
        {
            Name = ".NET MAUI",
            Category = SkillCategory.Mobile,
            Aliases = ["dotnet maui"]
        },

        new()
        {
            Name = "Entity Framework Core",
            Category = SkillCategory.Database,
            Aliases = ["ef core", "entity framework"]
        },

        new()
        {
            Name = "PostgreSQL",
            Category = SkillCategory.Database,
            Aliases = ["postgres"]
        },

        new()
        {
            Name = "SQLite",
            Category = SkillCategory.Database
        },

        new()
        {
            Name = "SQL",
            Category = SkillCategory.Language
        },

        new()
        {
            Name = "JavaScript",
            Category = SkillCategory.Language,
            Aliases = ["javascript", "js"]
        },

        new()
        {
            Name = "Node.js",
            Category = SkillCategory.Backend,
            Aliases = ["nodejs", "node"]
        },

        new()
        {
            Name = "Express",
            Category = SkillCategory.Backend,
            Aliases = ["express.js", "expressjs"]
        },

        new()
        {
            Name = "React",
            Category = SkillCategory.Frontend,
            Aliases = ["react.js", "reactjs"]
        },

        new()
        {
            Name = "Vite",
            Category = SkillCategory.Frontend
        },

        new()
        {
            Name = "HTML",
            Category = SkillCategory.Frontend
        },

        new()
        {
            Name = "CSS",
            Category = SkillCategory.Frontend
        },

        new()
        {
            Name = "Tailwind",
            Category = SkillCategory.Frontend,
            Aliases = ["tailwind css"]
        },

        new()
        {
            Name = "Git",
            Category = SkillCategory.Tool
        },

        new()
        {
            Name = "GitHub",
            Category = SkillCategory.Tool
        },

        new()
        {
            Name = "Docker",
            Category = SkillCategory.DevOps
        },

        new()
        {
            Name = "Swagger",
            Category = SkillCategory.Tool
        },

        new()
        {
            Name = "OpenAPI",
            Category = SkillCategory.Tool
        },

        new()
        {
            Name = "JWT",
            Category = SkillCategory.Backend,
            Aliases = ["json web token"]
        },

        new()
        {
            Name = "REST",
            Category = SkillCategory.Backend,
            Aliases = ["rest api", "api rest", "restful"]
        },

        new()
        {
            Name = "MVVM",
            Category = SkillCategory.Architecture
        },

        new()
        {
            Name = "SOLID",
            Category = SkillCategory.Architecture
        },

        new()
        {
            Name = "Clean Architecture",
            Category = SkillCategory.Architecture
        },

        new()
        {
            Name = "Dependency Injection",
            Category = SkillCategory.Architecture
        },

        new()
        {
            Name = "Repository Pattern",
            Category = SkillCategory.Architecture
        },

        new()
        {
            Name = "CommunityToolkit.Mvvm",
            Category = SkillCategory.Mobile,
            Aliases = ["communitytoolkit mvvm"]
        },

        new()
        {
            Name = "Refit",
            Category = SkillCategory.Mobile
        },

        new()
        {
            Name = "FluentValidation",
            Category = SkillCategory.Backend
        },

        new()
        {
            Name = "Serilog",
            Category = SkillCategory.Tool
        },

        new()
        {
            Name = "xUnit",
            Category = SkillCategory.Testing
        },

        new()
        {
            Name = "Moq",
            Category = SkillCategory.Testing
        },

        new()
        {
            Name = "Azure",
            Category = SkillCategory.Cloud
        },

        new()
        {
            Name = "Linux",
            Category = SkillCategory.OperatingSystem
        },

        new()
        {
            Name = "Windows",
            Category = SkillCategory.OperatingSystem
        },

        new()
        {
            Name = "Android",
            Category = SkillCategory.Mobile
        }
    ];
}
