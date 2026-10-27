using NeoTracker.Core.Models;

namespace NeoTracker.Core.Abstractions;

/// <summary>
/// Durable local storage of successfully received daily results, one entry per date.
/// </summary>
public interface IDailyResultCache
{
    Task<IReadOnlyList<CachedDailyResult>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stores <paramref name="entry"/>, replacing any entry for the same date.
    /// Returns <c>false</c> when the entry could not be persisted; the previously
    /// persisted cache is then left unchanged.
    /// </summary>
    Task<bool> SaveAsync(CachedDailyResult entry, CancellationToken cancellationToken);
}
