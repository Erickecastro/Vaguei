using System.Text.Json;
using Vaguei.Application.Interfaces;

namespace Vaguei.Infrastructure;

public sealed class JsonFavoriteJobStore : IFavoriteJobStore
{
    private readonly string _path;

    public JsonFavoriteJobStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vaguei",
            "favorites.json");
    }

    public IReadOnlySet<string> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var values = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path)) ?? [];
            return values.Where(value => !string.IsNullOrWhiteSpace(value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Save(IReadOnlySet<string> favoriteKeys)
    {
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(favoriteKeys.Order()));
        File.Move(temporaryPath, _path, overwrite: true);
    }
}
