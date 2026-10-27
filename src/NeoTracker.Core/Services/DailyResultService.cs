using System.Globalization;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;

namespace NeoTracker.Core.Services;

/// <summary>
/// Resolves the display outcome for a requested date.
/// </summary>
public sealed class DailyResultService(INasaNeoClient nasaClient, IDailyResultCache cache) : IDailyResultService
{
    public async Task<DailyResultOutcome> GetForDateAsync(string? rawDate, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(rawDate, SelectedDate.Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return new DailyResultOutcome.InvalidDate("Enter a real date in YYYY-MM-DD format.");

        var date = new SelectedDate(parsed);
        var result = await nasaClient.GetDailyResultAsync(date, cancellationToken);

        return result.IsEmpty
            ? new DailyResultOutcome.NoResultsNoCache(date, result)
            : new DailyResultOutcome.Fresh(date, result);
    }

    public Task<CachedDailyResult?> GetLatestCachedAsync(CancellationToken cancellationToken) =>
        Task.FromResult<CachedDailyResult?>(null);
}
