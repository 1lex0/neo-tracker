using NeoTracker.Core.Validation;

namespace NeoTracker.Core.Models;

/// <summary>
/// The display state chosen by the business logic for one request (see data-model.md).
/// </summary>
public abstract record DailyResultOutcome
{
    private DailyResultOutcome() { }

    /// <summary>Non-empty response received for the requested date.</summary>
    public sealed record Fresh(SelectedDate RequestedDate, DailyResult Result) : DailyResultOutcome;

    /// <summary>Requested date had no observations; latest eligible cached day is shown automatically.</summary>
    public sealed record NoResultsFallback(SelectedDate RequestedDate, DailyResult Response, CachedDailyResult Fallback) : DailyResultOutcome;

    /// <summary>Requested date had no observations and no eligible cached day exists.</summary>
    public sealed record NoResultsNoCache(SelectedDate RequestedDate, DailyResult Response) : DailyResultOutcome;

    /// <summary>NASA could not be reached or returned an unusable response; the cache is only offered.</summary>
    public sealed record UpstreamFailure(SelectedDate RequestedDate, string Reason, CachedDailyResult? Offer) : DailyResultOutcome;

    /// <summary>The input is not a real date in YYYY-MM-DD form; nothing was requested.</summary>
    public sealed record InvalidDate(DateValidationError Error, string Message) : DailyResultOutcome;
}
