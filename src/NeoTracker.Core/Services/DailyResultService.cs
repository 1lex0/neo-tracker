using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;
using NeoTracker.Core.Validation;

namespace NeoTracker.Core.Services;

/// <summary>
/// Resolves the display outcome for a requested date: persists every successful NASA
/// response, falls back automatically to the latest-received non-empty cached day when
/// NASA has no observations, and only offers that day when NASA is unavailable.
/// </summary>
public sealed class DailyResultService(INasaNeoClient nasaClient, IDailyResultCache cache) : IDailyResultService
{
    public async Task<DailyResultOutcome> GetForDateAsync(string? rawDate, CancellationToken cancellationToken)
    {
        // Validation runs before any cache access or NASA request.
        var validation = SelectedDateValidator.Validate(rawDate);
        if (validation.Date is not { } date)
            return new DailyResultOutcome.InvalidDate(validation.Error, validation.Message!);

        DailyResult result;
        try
        {
            result = await nasaClient.GetDailyResultAsync(date, cancellationToken);
        }
        catch (NasaUnavailableException ex)
        {
            return new DailyResultOutcome.UpstreamFailure(date, ex.Message, await GetLatestCachedAsync(cancellationToken));
        }

        var entries = await cache.GetAllAsync(cancellationToken);
        await PersistAsync(result, entries, cancellationToken);

        if (!result.IsEmpty)
            return new DailyResultOutcome.Fresh(date, result);

        // The just-received day is empty, so it can never be the fallback itself.
        var fallback = SelectFallback(entries);
        return fallback is null
            ? new DailyResultOutcome.NoResultsNoCache(date, result)
            : new DailyResultOutcome.NoResultsFallback(date, result, fallback);
    }

    public async Task<CachedDailyResult?> GetLatestCachedAsync(CancellationToken cancellationToken) =>
        SelectFallback(await cache.GetAllAsync(cancellationToken));

    /// <summary>
    /// Chooses the cached day with at least one observation that was received most
    /// recently, regardless of its calendar date relative to the requested date.
    /// </summary>
    public static CachedDailyResult? SelectFallback(IEnumerable<CachedDailyResult> entries) =>
        entries.Where(e => !e.Result.IsEmpty).MaxBy(e => e.CacheReceivedAtUtc);

    private async Task PersistAsync(DailyResult result, IReadOnlyList<CachedDailyResult> entries, CancellationToken cancellationToken)
    {
        // An empty day is retained, but never at the cost of a non-empty day for the same date.
        if (result.IsEmpty && entries.Any(e => e.Result.Date == result.Date && !e.Result.IsEmpty))
            return;

        // A failed write is reported by the cache and must not hide a successful NASA response.
        await cache.SaveAsync(new CachedDailyResult(result, result.ReceivedAtUtc), cancellationToken);
    }
}
