using Vaguei.Domain.Models;

namespace Vaguei.Desktop.ViewModels;

public sealed class JobResultItemViewModel
{
    public JobResultItemViewModel(
        JobMatchResult result,
        bool showCompatibility = true)
    {
        Title = result.Job.Title;
        Company = result.Job.Company;
        Location = result.Job.Location.RawLocation ?? "Local não informado";
        WorkModel = result.Job.WorkModel.ToString();
        Score = $"{result.Score:F0}%";
        ShowCompatibility = showCompatibility;
        Source = result.Job.Source ?? "Fonte não informada";
        Url = result.Job.Url?.AbsoluteUri ?? string.Empty;
        Reasons = showCompatibility
            ? string.Join(
                Environment.NewLine,
                result.Reasons
                    .Where(reason =>
                        reason.Criterion !=
                        Vaguei.Domain.Enums.JobMatchCriterion.ProfessionalRole)
                    .Select(reason => $"• {reason.Description}"))
            : string.Empty;

        Skills = showCompatibility
            ? result.Reasons
            .Where(reason =>
                reason.Criterion ==
                    Vaguei.Domain.Enums.JobMatchCriterion.Skill &&
                reason.Kind ==
                    Vaguei.Domain.Enums.JobMatchReasonKind.Positive)
            .Select(reason => reason.Description)
            .Where(description =>
                description.StartsWith(
                    "Competência compatível: ",
                    StringComparison.Ordinal))
            .Select(description =>
                description["Competência compatível: ".Length..]
                    .TrimEnd('.'))
            .ToArray()
            : [];

        Published = FormatPublishedAt(
            result.Job.PublishedAt,
            DateTimeOffset.UtcNow);
    }

    public string Title { get; }

    public string Company { get; }

    public string Location { get; }

    public string WorkModel { get; }

    public string Score { get; }

    public bool ShowCompatibility { get; }

    public bool HasReasons => !string.IsNullOrWhiteSpace(Reasons);

    public bool HasSkills => Skills.Count > 0;

    public string Source { get; }

    public string Url { get; }

    public string Reasons { get; }

    public IReadOnlyCollection<string> Skills { get; }

    public string Published { get; }

    private static string FormatPublishedAt(
        DateTimeOffset? publishedAt,
        DateTimeOffset referenceTime)
    {
        if (publishedAt is null)
        {
            return "Data não informada";
        }

        var elapsed = referenceTime - publishedAt.Value;

        if (elapsed.TotalHours < 1)
        {
            return "Publicada há menos de 1 hora";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"Publicada há {(int)elapsed.TotalHours} horas";
        }

        var days = (int)elapsed.TotalDays;

        return days == 1
            ? "Publicada há 1 dia"
            : $"Publicada há {days} dias";
    }
}
