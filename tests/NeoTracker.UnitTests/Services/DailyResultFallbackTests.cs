using NeoTracker.Core.Services;
using NeoTracker.UnitTests.Fixtures;

namespace NeoTracker.UnitTests.Services;

public class DailyResultFallbackTests
{
    [Fact]
    public void SelectFallback_ReturnsNonEmptyEntryWithLatestReceiptTime()
    {
        var older = TestData.Cached(TestData.Date(2026, 10, 20), TestData.Utc(2026, 10, 21), 2);
        var latest = TestData.Cached(TestData.Date(2026, 10, 10), TestData.Utc(2026, 10, 25), 1);
        var middle = TestData.Cached(TestData.Date(2026, 10, 15), TestData.Utc(2026, 10, 23), 3);

        Assert.Same(latest, DailyResultService.SelectFallback([older, latest, middle]));
    }

    [Fact]
    public void SelectFallback_IgnoresCalendarDateOrdering_EvenWhenLaterThanRequestedDate()
    {
        // The latest-received non-empty day wins although its date is in the future
        // relative to an older entry with a closer date.
        var closeDateReceivedEarlier = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 1);
        var laterDateReceivedLast = TestData.Cached(TestData.Date(2027, 1, 1), TestData.Utc(2026, 10, 27), 1);

        Assert.Same(laterDateReceivedLast, DailyResultService.SelectFallback([closeDateReceivedEarlier, laterDateReceivedLast]));
    }

    [Fact]
    public void SelectFallback_IgnoresEmptyDaysEvenWhenReceivedMoreRecently()
    {
        var nonEmpty = TestData.Cached(TestData.Date(2026, 10, 20), TestData.Utc(2026, 10, 20), 1);
        var newerEmpty = TestData.Cached(TestData.Date(2026, 10, 21), TestData.Utc(2026, 10, 27), 0);

        Assert.Same(nonEmpty, DailyResultService.SelectFallback([nonEmpty, newerEmpty]));
    }

    [Fact]
    public void SelectFallback_OnlyEmptyDays_ReturnsNull()
    {
        var empty1 = TestData.Cached(TestData.Date(2026, 10, 20), TestData.Utc(2026, 10, 20), 0);
        var empty2 = TestData.Cached(TestData.Date(2026, 10, 21), TestData.Utc(2026, 10, 21), 0);

        Assert.Null(DailyResultService.SelectFallback([empty1, empty2]));
    }

    [Fact]
    public void SelectFallback_EmptyCache_ReturnsNull()
    {
        Assert.Null(DailyResultService.SelectFallback([]));
    }
}
