using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed class JobSearchQuery
{
    public List<string> Keywords { get; set; } = [];

    public List<string> Locations { get; set; } = [];

    public List<WorkModel> WorkModels { get; set; } = [];

    public List<EmploymentType> EmploymentTypes { get; set; } = [];

    public List<SeniorityLevel> SeniorityLevels { get; set; } = [];
}