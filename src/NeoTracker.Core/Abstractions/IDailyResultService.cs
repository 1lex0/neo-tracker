using NeoTracker.Core.Models;

namespace NeoTracker.Core.Abstractions;

public interface IDailyResultService
{
    /// <summary>Validates <paramref name="rawDate"/> and resolves the display outcome for it.</summary>
    Task<DailyResultOutcome> GetForDateAsync(string? rawDate, CancellationToken cancellationToken);

    /// <summary>Latest-received cached day with at least one observation, if any.</summary>
    Task<CachedDailyResult?> GetLatestCachedAsync(CancellationToken cancellationToken);
}
