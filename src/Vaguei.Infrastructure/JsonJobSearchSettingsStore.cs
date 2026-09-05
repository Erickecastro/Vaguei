using System.Text.Json;
using Vaguei.Application.Interfaces;
using Vaguei.Application.Models;

namespace Vaguei.Infrastructure;

public sealed class JsonJobSearchSettingsStore : IJobSearchSettingsStore
{
    private readonly string _path;

    public JsonJobSearchSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vaguei",
            "search-settings.json");
    }

    public JobSearchSettings Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<JobSearchSettings>(File.ReadAllText(_path)) ?? new()
                : new();
        }
        catch (JsonException) { return new(); }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
    }

    public void Save(JobSearchSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings));
        File.Move(temporaryPath, _path, overwrite: true);
    }
}
