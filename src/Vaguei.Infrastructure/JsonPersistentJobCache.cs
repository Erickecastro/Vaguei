using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vaguei.Application.Interfaces;
using Vaguei.Domain.Entities;

namespace Vaguei.Infrastructure;

public sealed class JsonPersistentJobCache : IPersistentJobCache
{
    private const int MaximumEntries = 128;
    private readonly string _directory;
    private readonly object _sync = new();

    public JsonPersistentJobCache(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vaguei",
            "job-cache");
    }

    public bool TryLoad(
        string source,
        string queryKey,
        out IReadOnlyCollection<JobPosting> jobs,
        out DateTimeOffset expiresAt)
    {
        lock (_sync)
        {
            try
            {
                var path = GetPath(source, queryKey);
                if (!File.Exists(path))
                {
                    jobs = [];
                    expiresAt = default;
                    return false;
                }

                var entry = JsonSerializer.Deserialize<CacheDocument>(
                    File.ReadAllText(path));
                if (entry is null || entry.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    File.Delete(path);
                    jobs = [];
                    expiresAt = default;
                    return false;
                }

                jobs = entry.Jobs;
                expiresAt = entry.ExpiresAt;
                return true;
            }
            catch (JsonException)
            {
                jobs = [];
                expiresAt = default;
                return false;
            }
            catch (IOException)
            {
                jobs = [];
                expiresAt = default;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                jobs = [];
                expiresAt = default;
                return false;
            }
        }
    }

    public void Save(
        string source,
        string queryKey,
        DateTimeOffset expiresAt,
        IReadOnlyCollection<JobPosting> jobs)
    {
        lock (_sync)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                Prune();
                var path = GetPath(source, queryKey);
                var temporaryPath = $"{path}.tmp";
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(new CacheDocument(expiresAt, jobs.ToArray())));
                File.Move(temporaryPath, path, overwrite: true);
            }
            catch (IOException)
            {
                // Cache é uma otimização: falhas de disco não interrompem a busca.
            }
            catch (UnauthorizedAccessException)
            {
                // Cache é uma otimização: falhas de permissão não interrompem a busca.
            }
        }
    }

    private string GetPath(string source, string queryKey)
    {
        var identity = Encoding.UTF8.GetBytes($"{source}\u001f{queryKey}");
        return Path.Combine(_directory, $"{Convert.ToHexString(SHA256.HashData(identity))}.json");
    }

    private void Prune()
    {
        var files = new DirectoryInfo(_directory)
            .EnumerateFiles("*.json")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        foreach (var file in files.Skip(MaximumEntries - 1))
        {
            file.Delete();
        }
    }

    private sealed record CacheDocument(
        DateTimeOffset ExpiresAt,
        JobPosting[] Jobs);
}
