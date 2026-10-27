using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;

namespace NeoTracker.Api.Persistence;

/// <summary>
/// Persists successfully received daily results in a single local JSON file, one entry
/// per date. Writers are serialized in-process, and every write goes through a unique
/// temporary file in the same directory that then replaces the active file, so an
/// interrupted or failed write never leaves a partially written cache.
/// </summary>
public sealed class JsonDailyResultCache : IDailyResultCache
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly Func<string, string> _tempFilePathFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<DateOnly, CachedDailyResult>? _entries;

    private readonly ILogger _logger;

    public JsonDailyResultCache(string filePath, ILogger<JsonDailyResultCache>? logger = null)
        : this(filePath, DefaultTempFilePath, logger)
    {
    }

    internal JsonDailyResultCache(string filePath, Func<string, string> tempFilePathFactory, ILogger<JsonDailyResultCache>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
        _tempFilePathFactory = tempFilePathFactory;
        _logger = logger ?? NullLogger<JsonDailyResultCache>.Instance;
    }

    public string FilePath => _filePath;

    public async Task<IReadOnlyList<CachedDailyResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync()).Values.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SaveAsync(CachedDailyResult entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var updated = new Dictionary<DateOnly, CachedDailyResult>(await LoadAsync())
            {
                [entry.Result.Date] = entry,
            };

            try
            {
                // A client disconnect must not abort a write that is already under way.
                await WriteAsync(updated, CancellationToken.None);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                _logger.LogError(ex, "Failed to write NEO cache file {CacheFile} for {Date}; previous cache kept", _filePath, entry.Result.Date);
                return false;
            }

            _entries = updated;
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<DateOnly, CachedDailyResult>> LoadAsync()
    {
        if (_entries is not null)
            return _entries;

        var entries = new Dictionary<DateOnly, CachedDailyResult>();
        if (File.Exists(_filePath))
        {
            try
            {
                await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var file = await JsonSerializer.DeserializeAsync<CacheFile>(stream, SerializerOptions);
                foreach (var entry in file?.Entries ?? [])
                {
                    if (entry?.Result is not null)
                        entries[entry.Result.Date] = entry;
                }
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // An unreadable cache must not take the application down. Keep a copy so
                // the next successful write does not silently destroy it.
                _logger.LogError(ex, "Failed to read NEO cache file {CacheFile}; starting with an empty cache", _filePath);
                PreserveUnreadableFile();
                entries.Clear();
            }
        }

        _entries = entries;
        return entries;
    }

    private async Task WriteAsync(Dictionary<DateOnly, CachedDailyResult> entries, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var tempPath = _tempFilePathFactory(_filePath);
        var file = new CacheFile(CurrentVersion, entries.Values.OrderBy(e => e.Result.Date).ToList<CachedDailyResult?>());

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, file, SerializerOptions, cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private void PreserveUnreadableFile()
    {
        try
        {
            File.Copy(_filePath, $"{_filePath}.unreadable", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not preserve unreadable NEO cache file {CacheFile}", _filePath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string DefaultTempFilePath(string filePath) =>
        Path.Combine(Path.GetDirectoryName(filePath)!, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

    private sealed record CacheFile(int Version, List<CachedDailyResult?>? Entries);
}
