using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;

namespace Vaguei.Application.Services;

public sealed class JobSearchQueryBuilder
{
    public JobSearchQuery Build(
        CandidateProfile profile,
        JobSearchPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(preferences);

        var query = new JobSearchQuery();

        AddKeywords(
            query,
            profile,
            preferences);

        AddLocations(
            query,
            preferences);

        query.WorkModels.AddRange(
            preferences.WorkModels);

        query.EmploymentTypes.AddRange(
            preferences.EmploymentTypes);

        query.SeniorityLevels.AddRange(
            preferences.SeniorityLevels);

        return query;
    }

    private static void AddKeywords(
        JobSearchQuery query,
        CandidateProfile profile,
        JobSearchPreferences preferences)
    {
        if (preferences.DesiredRoles.Count > 0)
        {
            foreach (var role in preferences.DesiredRoles)
            {
                AddDistinct(
                    query.Keywords,
                    role);
            }

            return;
        }

        AddDistinct(
            query.Keywords,
            profile.ProfessionalTitle);
    }

    private static void AddLocations(
        JobSearchQuery query,
        JobSearchPreferences preferences)
    {
        foreach (var country in preferences.Countries)
        {
            AddDistinct(
                query.Locations,
                country);
        }

        foreach (var state in preferences.States)
        {
            AddDistinct(
                query.Locations,
                state);
        }

        foreach (var city in preferences.Cities)
        {
            AddDistinct(
                query.Locations,
                city);
        }
    }

    private static void AddDistinct(
        List<string> values,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalizedValue = value.Trim();

        if (values.Any(
                existing =>
                    existing.Equals(
                        normalizedValue,
                        StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        values.Add(normalizedValue);
    }
}