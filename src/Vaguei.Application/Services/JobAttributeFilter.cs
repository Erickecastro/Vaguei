using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobAttributeFilter
{
    public IReadOnlyCollection<JobPosting> Filter(
        IEnumerable<JobPosting> jobs,
        JobSearchPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(preferences);

        return jobs.Where(job =>
                Matches(job.WorkModel, preferences.WorkModels) &&
                Matches(job.EmploymentType, preferences.EmploymentTypes) &&
                Matches(GetSeniority(job), preferences.SeniorityLevels))
            .ToArray();
    }

    private static bool Matches<T>(T value, IReadOnlySet<T> requested)
        where T : struct, Enum =>
        requested.Count == 0 || requested.Contains(value);

    private static SeniorityLevel GetSeniority(JobPosting job)
    {
        if (job.SeniorityLevel != SeniorityLevel.Unknown)
        {
            return job.SeniorityLevel;
        }

        var text = $"{job.Title} {job.Description}";

        if (ContainsAny(text, "estágio", "estagio", "internship", " intern "))
        {
            return SeniorityLevel.Internship;
        }

        if (ContainsAny(text, "trainee"))
        {
            return SeniorityLevel.Trainee;
        }

        if (ContainsAny(text, "júnior", "junior", " jr ", "jr."))
        {
            return SeniorityLevel.Junior;
        }

        if (ContainsAny(text, "pleno", "mid-level", "mid level", " intermediate "))
        {
            return SeniorityLevel.MidLevel;
        }

        if (ContainsAny(text, "sênior", "senior", " sr ", "sr."))
        {
            return SeniorityLevel.Senior;
        }

        return ContainsAny(text, "tech lead", "team lead", "líder", "lider", "principal", "staff")
            ? SeniorityLevel.Lead
            : SeniorityLevel.Unknown;
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        var padded = $" {text} ";
        return terms.Any(term => padded.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
