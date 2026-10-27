using NeoTracker.Core.Models;

namespace NeoTracker.UnitTests.Fixtures;

public static class TestData
{
    public static SelectedDate Date(int year, int month, int day) => new(new DateOnly(year, month, day));

    public static DateTimeOffset Utc(int year, int month, int day, int hour = 12) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    public static AsteroidObservation Asteroid(string id = "1", bool hazardous = false) =>
        new(id, $"({id})", 10, 20, 45000, 1_000_000, hazardous);

    public static DailyResult Day(SelectedDate date, DateTimeOffset receivedAt, int observations) =>
        new(date.Value,
            Enumerable.Range(1, observations).Select(i => Asteroid($"{date}-{i}")).ToList(),
            DailyResult.NasaSource,
            receivedAt);

    public static CachedDailyResult Cached(SelectedDate date, DateTimeOffset receivedAt, int observations) =>
        new(Day(date, receivedAt, observations), receivedAt);
}
