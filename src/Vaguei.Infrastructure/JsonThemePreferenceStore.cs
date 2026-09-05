using System.Text.Json;
using Vaguei.Application.Interfaces;

namespace Vaguei.Infrastructure;

public sealed class JsonThemePreferenceStore : IThemePreferenceStore
{
    private readonly string _path;

    public JsonThemePreferenceStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vaguei",
            "appearance.json");
    }

    public string? Load()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            var settings = JsonSerializer.Deserialize<AppearanceSettings>(File.ReadAllText(_path));
            return settings?.Theme is "Light" or "Dark" ? settings.Theme : null;
        }
        catch (JsonException) { return null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    public void Save(string theme)
    {
        if (theme is not ("Light" or "Dark"))
            throw new ArgumentException("Tema inválido.", nameof(theme));

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new AppearanceSettings(theme)));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    private sealed record AppearanceSettings(string Theme);
}
