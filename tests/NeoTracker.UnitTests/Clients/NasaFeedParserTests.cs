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

    [Fact]
    public void Parse_ValidEmptyFeed_ReturnsSuccessfulEmptyDay()
    {
        const string json = """{ "element_count": 0, "near_earth_objects": { "2026-10-27": [] } }""";

        var result = NasaFeedParser.Parse(json, RequestedDate, ReceivedAt);

        Assert.True(result.IsEmpty);
        Assert.Equal(RequestedDate.Value, result.Date);
        Assert.Equal(ReceivedAt, result.ReceivedAtUtc);
    }

    [Fact]
    public void Parse_FeedWithoutRequestedDateKey_ReturnsSuccessfulEmptyDay()
    {
        const string json = """{ "element_count": 0, "near_earth_objects": {} }""";

        Assert.True(NasaFeedParser.Parse(json, RequestedDate, ReceivedAt).IsEmpty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{ \"near_earth_objects\": ")]
    [InlineData("[]")]
    [InlineData("null")]
    public void Parse_MalformedJson_Throws(string json)
    {
        Assert.Throws<NasaFeedFormatException>(() => NasaFeedParser.Parse(json, RequestedDate, ReceivedAt));
    }

    [Theory]
    [InlineData("""{ "element_count": 0 }""")]
    [InlineData("""{ "near_earth_objects": [] }""")]
    [InlineData("""{ "near_earth_objects": { "2026-10-27": {} } }""")]
    public void Parse_MissingOrMistypedFeedStructure_Throws(string json)
    {
        Assert.Throws<NasaFeedFormatException>(() => NasaFeedParser.Parse(json, RequestedDate, ReceivedAt));
    }

    public static TheoryData<string> BrokenAsteroids => new()
    {
        // missing name
        """{ "id": "1", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
        // missing id
        """{ "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
        // missing diameter in kilometers
        """{ "id": "1", "name": "A", "estimated_diameter": { "meters": { "estimated_diameter_min": 100, "estimated_diameter_max": 200 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
        // missing hazard flag
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
        // hazard flag is not a boolean
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": "yes", "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
        // no close approach data
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [] }""",
        // missing velocity
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "miss_distance": { "kilometers": "1" } } ] }""",
        // non-numeric distance
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "far" } } ] }""",
        // negative diameter
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": -0.1, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
        // maximum below minimum
        """{ "id": "1", "name": "A", "estimated_diameter": { "kilometers": { "estimated_diameter_min": 0.3, "estimated_diameter_max": 0.2 } }, "is_potentially_hazardous_asteroid": false, "close_approach_data": [ { "close_approach_date": "2026-10-27", "relative_velocity": { "kilometers_per_hour": "1" }, "miss_distance": { "kilometers": "1" } } ] }""",
    };

    [Theory]
    [MemberData(nameof(BrokenAsteroids))]
    public void Parse_AsteroidWithMissingOrInvalidRequiredField_Throws(string asteroid)
    {
        var json = $$"""{ "near_earth_objects": { "2026-10-27": [ {{asteroid}} ] } }""";

        Assert.Throws<NasaFeedFormatException>(() => NasaFeedParser.Parse(json, RequestedDate, ReceivedAt));
    }
}
