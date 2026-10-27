namespace NeoTracker.Core.Models;

/// <summary>
/// A successfully received daily result retained locally across restarts.
/// </summary>
public sealed record CachedDailyResult
{
    public CachedDailyResult(DailyResult result, DateTimeOffset cacheReceivedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
        CacheReceivedAtUtc = cacheReceivedAtUtc;
    }

    public DailyResult Result { get; }

    /// <summary>Orders cached entries by when they were received.</summary>
    public DateTimeOffset CacheReceivedAtUtc { get; }
}
