namespace NeoTracker.UnitTests.Fixtures;

/// <summary>
/// An isolated, self-deleting directory for cache files used by a single test.
/// </summary>
public sealed class TemporaryCacheDirectory : IDisposable
{
    public TemporaryCacheDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "neo-tracker-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CacheFilePath => System.IO.Path.Combine(Path, "neo-cache.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
