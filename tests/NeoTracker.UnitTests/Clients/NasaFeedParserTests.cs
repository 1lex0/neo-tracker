using NeoTracker.Core.Models;
using NeoTracker.Core.Services;
using NeoTracker.UnitTests.Fixtures;

namespace NeoTracker.UnitTests.Clients;

public class NasaFeedParserTests
{
    private static readonly SelectedDate RequestedDate = new(new DateOnly(2026, 10, 27));
    private static readonly DateTimeOffset ReceivedAt = new(2026, 10, 27, 21, 40, 0, TimeSpan.Zero);

    private static DailyResult ParseSample() =>
        NasaFeedParser.Parse(FixtureFiles.NasaFeedSample, RequestedDate, ReceivedAt);

    [Fact]
    public void Parse_SelectsOnlyObservationsForRequestedDateKey()
    {
        var result = ParseSample();

        Assert.Equal(RequestedDate.Value, result.Date);
        Assert.Equal(["2465633", "3426410", "54016717"], result.Observations.Select(o => o.Id));
        Assert.DoesNotContain(result.Observations, o => o.Id == "9999999");
    }

    [Fact]
    public void Parse_SetsSourceAndReceiptTime()
    {
        var result = ParseSample();

        Assert.Equal(DailyResult.NasaSource, result.Source);
        Assert.Equal(ReceivedAt, result.ReceivedAtUtc);
    }

    [Fact]
    public void Parse_MapsRequiredValuesAndConvertsDiameterKilometersToMeters()
    {
        var hazardous = ParseSample().Observations[0];

        Assert.Equal("465633 (2009 JR5)", hazardous.Name);
        Assert.Equal(217.0475943, hazardous.EstimatedDiameterMinMeters, precision: 6);
        Assert.Equal(485.3331752, hazardous.EstimatedDiameterMaxMeters, precision: 6);
        Assert.Equal(65260.6371983344, hazardous.RelativeVelocityKph, precision: 6);
        Assert.Equal(45290438.204452618, hazardous.MissDistanceKm, precision: 3);
        Assert.True(hazardous.IsPotentiallyHazardous);
    }

    [Fact]
    public void Parse_PreservesNonHazardousFlag()
    {
        var observations = ParseSample().Observations;

        Assert.False(observations[1].IsPotentiallyHazardous);
        Assert.False(observations[2].IsPotentiallyHazardous);
    }

    [Fact]
    public void Parse_UsesCloseApproachForRequestedDateWhenSeveralAreListed()
    {
        var observation = ParseSample().Observations[1];

        Assert.Equal(25329.9042945417, observation.RelativeVelocityKph, precision: 6);
        Assert.Equal(38767964.651138751, observation.MissDistanceKm, precision: 3);
    }
}
