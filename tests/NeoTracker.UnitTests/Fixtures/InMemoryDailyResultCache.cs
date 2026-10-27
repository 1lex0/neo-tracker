using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;

namespace NeoTracker.UnitTests.Fixtures;

public sealed class InMemoryDailyResultCache(params CachedDailyResult[] seed) : IDailyResultCache
{
    public Dictionary<DateOnly, CachedDailyResult> Entries { get; } = seed.ToDictionary(e => e.Result.Date);

    public int ReadCount { get; private set; }

    public Task<IReadOnlyList<CachedDailyResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        ReadCount++;
        return Task.FromResult<IReadOnlyList<CachedDailyResult>>(Entries.Values.ToList());
    }

    public Task<bool> SaveAsync(CachedDailyResult entry, CancellationToken cancellationToken)
    {
        Entries[entry.Result.Date] = entry;
        return Task.FromResult(true);
    }
}
