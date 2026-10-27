using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;

namespace NeoTracker.UnitTests.Fixtures;

public sealed class FakeNasaNeoClient(Func<SelectedDate, DailyResult> respond) : INasaNeoClient
{
    public List<SelectedDate> Requests { get; } = [];

    public static FakeNasaNeoClient Returning(Func<SelectedDate, DailyResult> respond) => new(respond);

    public static FakeNasaNeoClient Failing(string reason = "NASA NeoWs returned HTTP 503.") =>
        new(_ => throw new NasaUnavailableException(reason));

    public Task<DailyResult> GetDailyResultAsync(SelectedDate date, CancellationToken cancellationToken)
    {
        Requests.Add(date);
        return Task.FromResult(respond(date));
    }
}
