using System.Text.Json;

namespace Vaguei.Collectors.Configuration;

public sealed class JobSourceCatalog
{
    public Dictionary<string, string> Ashby { get; init; } = [];

    public Dictionary<string, string> Greenhouse { get; init; } = [];

    public Dictionary<string, string> InHire { get; init; } = [];

    public Dictionary<string, string> Lever { get; init; } = [];

    public Dictionary<string, string> SmartRecruiters { get; init; } = [];

    public Dictionary<string, string> Workable { get; init; } = [];

    public static JobSourceCatalog Load(string? path = null)
    {
        var catalogPath = path ?? Path.Combine(
            AppContext.BaseDirectory,
            "job-sources.json");

        try
        {
            if (!File.Exists(catalogPath))
            {
                return CreateDefault();
            }

            using var stream = File.OpenRead(catalogPath);
            var configured = JsonSerializer.Deserialize<JobSourceCatalog>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return configured is null
                ? CreateDefault()
                : Validate(configured);
        }
        catch (JsonException)
        {
            return CreateDefault();
        }
        catch (IOException)
        {
            return CreateDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return CreateDefault();
        }
    }

    public static JobSourceCatalog CreateDefault() => new()
    {
        Ashby = Clone(Sources.AshbyJobSource.DefaultBoards),
        Greenhouse = Clone(Sources.GreenhouseJobSource.DefaultBoards),
        InHire = Clone(Sources.InHireJobSource.DefaultTenants),
        Lever = Clone(Sources.LeverJobSource.DefaultSites),
        SmartRecruiters = Clone(Sources.SmartRecruitersJobSource.DefaultCompanies),
        Workable = Clone(Sources.WorkableJobSource.DefaultAccounts)
    };

    private static JobSourceCatalog Validate(JobSourceCatalog configured)
    {
        var defaults = CreateDefault();

        return new JobSourceCatalog
        {
            Ashby = NormalizeOrDefault(configured.Ashby, defaults.Ashby),
            Greenhouse = NormalizeOrDefault(configured.Greenhouse, defaults.Greenhouse),
            InHire = NormalizeOrDefault(configured.InHire, defaults.InHire),
            Lever = NormalizeOrDefault(configured.Lever, defaults.Lever),
            SmartRecruiters = NormalizeOrDefault(
                configured.SmartRecruiters,
                defaults.SmartRecruiters),
            Workable = NormalizeOrDefault(configured.Workable, defaults.Workable)
        };
    }

    private static Dictionary<string, string> NormalizeOrDefault(
        IReadOnlyDictionary<string, string>? configured,
        IReadOnlyDictionary<string, string> defaults)
    {
        var validEntries = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        if (configured is not null)
        {
            foreach (var entry in configured)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) ||
                    string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                validEntries[entry.Key.Trim()] = entry.Value.Trim();
            }
        }

        return validEntries.Count > 0
            ? validEntries
            : Clone(defaults);
    }

    private static Dictionary<string, string> Clone(
        IReadOnlyDictionary<string, string> source) =>
        new(source, StringComparer.OrdinalIgnoreCase);
}
