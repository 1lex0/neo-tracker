using NeoTracker.Api.Persistence;
using NeoTracker.UnitTests.Fixtures;

namespace NeoTracker.UnitTests.Persistence;

public class JsonDailyResultCacheTests
{
    [Fact]
    public async Task SavedEmptyAndNonEmptyDays_SurviveReloadInNewInstance()
    {
        using var directory = new TemporaryCacheDirectory();
        var nonEmpty = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);
        var empty = TestData.Cached(TestData.Date(2026, 10, 27), TestData.Utc(2026, 10, 27), 0);

        var writer = new JsonDailyResultCache(directory.CacheFilePath);
        Assert.True(await writer.SaveAsync(nonEmpty, CancellationToken.None));
        Assert.True(await writer.SaveAsync(empty, CancellationToken.None));

        // A new instance simulates an application restart.
        var reloaded = await new JsonDailyResultCache(directory.CacheFilePath).GetAllAsync(CancellationToken.None);

        Assert.Equal(2, reloaded.Count);
        var restoredNonEmpty = Assert.Single(reloaded, e => e.Result.Date == nonEmpty.Result.Date);
        Assert.Equal(nonEmpty.CacheReceivedAtUtc, restoredNonEmpty.CacheReceivedAtUtc);
        Assert.Equal(nonEmpty.Result.ReceivedAtUtc, restoredNonEmpty.Result.ReceivedAtUtc);
        Assert.Equal(nonEmpty.Result.Source, restoredNonEmpty.Result.Source);
        Assert.Equal(nonEmpty.Result.Observations, restoredNonEmpty.Result.Observations);
        Assert.True(Assert.Single(reloaded, e => e.Result.Date == empty.Result.Date).Result.IsEmpty);
    }

    [Fact]
    public async Task Save_SameDateReplacesPreviousEntry()
    {
        using var directory = new TemporaryCacheDirectory();
        var cache = new JsonDailyResultCache(directory.CacheFilePath);
        var date = TestData.Date(2026, 10, 26);

        await cache.SaveAsync(TestData.Cached(date, TestData.Utc(2026, 10, 26), 1), CancellationToken.None);
        await cache.SaveAsync(TestData.Cached(date, TestData.Utc(2026, 10, 27), 3), CancellationToken.None);

        var entry = Assert.Single(await new JsonDailyResultCache(directory.CacheFilePath).GetAllAsync(CancellationToken.None));
        Assert.Equal(3, entry.Result.Observations.Count);
    }

    [Fact]
    public async Task FailedWrite_ReturnsFalseAndKeepsPreviousCache()
    {
        using var directory = new TemporaryCacheDirectory();
        var original = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);
        await new JsonDailyResultCache(directory.CacheFilePath).SaveAsync(original, CancellationToken.None);
        var before = await File.ReadAllTextAsync(directory.CacheFilePath);

        // The temporary file cannot be created, so the write fails before replacing anything.
        var failing = new JsonDailyResultCache(
            directory.CacheFilePath,
            _ => Path.Combine(directory.Path, "missing-directory", "cache.tmp"));
        var saved = await failing.SaveAsync(TestData.Cached(TestData.Date(2026, 10, 27), TestData.Utc(2026, 10, 27), 5), CancellationToken.None);

        Assert.False(saved);
        Assert.Equal(before, await File.ReadAllTextAsync(directory.CacheFilePath));
        var entries = await failing.GetAllAsync(CancellationToken.None);
        Assert.Equal(original.Result.Date, Assert.Single(entries).Result.Date);
    }

    [Fact]
    public async Task Save_LeavesNoTemporaryFilesBehind()
    {
        using var directory = new TemporaryCacheDirectory();
        var cache = new JsonDailyResultCache(directory.CacheFilePath);

        await cache.SaveAsync(TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 1), CancellationToken.None);

        Assert.Equal([directory.CacheFilePath], Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task MissingFile_LoadsAsEmptyCache()
    {
        using var directory = new TemporaryCacheDirectory();

        Assert.Empty(await new JsonDailyResultCache(directory.CacheFilePath).GetAllAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("""{ "version": 1, "entries": [ { "result": { "date": "2026-10-26", "observations": [ { "id": "", "name": "x", "estimatedDiameterMinMeters": 1, "estimatedDiameterMaxMeters": 2, "relativeVelocityKph": 1, "missDistanceKm": 1, "isPotentiallyHazardous": false } ], "source": "NASA NeoWs API", "receivedAtUtc": "2026-10-26T00:00:00+00:00" }, "cacheReceivedAtUtc": "2026-10-26T00:00:00+00:00" } ] }""")]
    public async Task UnreadableFile_LoadsAsEmptyAndPreservesCopy(string content)
    {
        using var directory = new TemporaryCacheDirectory();
        await File.WriteAllTextAsync(directory.CacheFilePath, content);

        var entries = await new JsonDailyResultCache(directory.CacheFilePath).GetAllAsync(CancellationToken.None);

        Assert.Empty(entries);
        Assert.Equal(content, await File.ReadAllTextAsync(directory.CacheFilePath + ".unreadable"));
    }

    [Fact]
    public async Task ConcurrentSaves_AllPersist()
    {
        using var directory = new TemporaryCacheDirectory();
        var cache = new JsonDailyResultCache(directory.CacheFilePath);

        await Task.WhenAll(Enumerable.Range(1, 20).Select(day =>
            cache.SaveAsync(TestData.Cached(TestData.Date(2026, 10, day), TestData.Utc(2026, 10, day), 1), CancellationToken.None)));

        Assert.Equal(20, (await new JsonDailyResultCache(directory.CacheFilePath).GetAllAsync(CancellationToken.None)).Count);
    }
}
