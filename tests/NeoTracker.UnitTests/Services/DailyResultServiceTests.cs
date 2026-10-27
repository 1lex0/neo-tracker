using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NeoTracker.Api.Endpoints;
using NeoTracker.Core.Models;
using NeoTracker.Core.Services;
using NeoTracker.UnitTests.Fixtures;

namespace NeoTracker.UnitTests.Services;

public class DailyResultServiceTests
{
    private static readonly SelectedDate Requested = TestData.Date(2026, 10, 27);
    private static readonly DateTimeOffset Now = TestData.Utc(2026, 10, 27, 21);

    [Fact]
    public async Task NonEmptyResponse_IsFreshAndPersisted()
    {
        var nasa = FakeNasaNeoClient.Returning(d => TestData.Day(d, Now, 2));
        var cache = new InMemoryDailyResultCache();

        var outcome = await new DailyResultService(nasa, cache).GetForDateAsync("2026-10-27", CancellationToken.None);

        var fresh = Assert.IsType<DailyResultOutcome.Fresh>(outcome);
        Assert.Equal(Requested, fresh.RequestedDate);
        Assert.Equal(2, fresh.Result.Observations.Count);
        Assert.Equal(Now, cache.Entries[Requested.Value].CacheReceivedAtUtc);
    }

    [Fact]
    public async Task EmptyResponse_IsPersistedAndFallsBackToLatestReceivedNonEmptyDay()
    {
        var earlierDateReceivedLast = TestData.Cached(TestData.Date(2026, 10, 1), TestData.Utc(2026, 10, 26), 1);
        var laterDateReceivedFirst = TestData.Cached(TestData.Date(2026, 10, 25), TestData.Utc(2026, 10, 25), 4);
        var cache = new InMemoryDailyResultCache(earlierDateReceivedLast, laterDateReceivedFirst);
        var nasa = FakeNasaNeoClient.Returning(d => TestData.Day(d, Now, 0));

        var outcome = await new DailyResultService(nasa, cache).GetForDateAsync("2026-10-27", CancellationToken.None);

        var fallback = Assert.IsType<DailyResultOutcome.NoResultsFallback>(outcome);
        Assert.Equal(Requested, fallback.RequestedDate);
        Assert.Same(earlierDateReceivedLast, fallback.Fallback);
        Assert.True(cache.Entries[Requested.Value].Result.IsEmpty);
    }

    [Fact]
    public async Task EmptyResponse_CanFallBackToDayLaterThanRequested()
    {
        var future = TestData.Cached(TestData.Date(2026, 12, 31), TestData.Utc(2026, 10, 26), 1);
        var nasa = FakeNasaNeoClient.Returning(d => TestData.Day(d, Now, 0));

        var outcome = await new DailyResultService(nasa, new InMemoryDailyResultCache(future)).GetForDateAsync("2026-10-27", CancellationToken.None);

        Assert.Same(future, Assert.IsType<DailyResultOutcome.NoResultsFallback>(outcome).Fallback);
    }

    [Fact]
    public async Task EmptyResponse_WithOnlyEmptyCachedDays_ReportsNoCachedResults()
    {
        var cache = new InMemoryDailyResultCache(TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 0));
        var nasa = FakeNasaNeoClient.Returning(d => TestData.Day(d, Now, 0));

        var outcome = await new DailyResultService(nasa, cache).GetForDateAsync("2026-10-27", CancellationToken.None);

        Assert.IsType<DailyResultOutcome.NoResultsNoCache>(outcome);
    }

    [Fact]
    public async Task EmptyResponse_DoesNotOverwriteNonEmptyCachedDayForSameDate()
    {
        var existing = TestData.Cached(Requested, TestData.Utc(2026, 10, 26), 3);
        var cache = new InMemoryDailyResultCache(existing);
        var nasa = FakeNasaNeoClient.Returning(d => TestData.Day(d, Now, 0));

        var outcome = await new DailyResultService(nasa, cache).GetForDateAsync("2026-10-27", CancellationToken.None);

        Assert.Same(existing, cache.Entries[Requested.Value]);
        Assert.Same(existing, Assert.IsType<DailyResultOutcome.NoResultsFallback>(outcome).Fallback);
    }

    [Fact]
    public async Task UpstreamFailure_OffersLatestNonEmptyCachedDayWithoutPersistingAnything()
    {
        var cached = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);
        var cache = new InMemoryDailyResultCache(cached);

        var outcome = await new DailyResultService(FakeNasaNeoClient.Failing(), cache).GetForDateAsync("2026-10-27", CancellationToken.None);

        var failure = Assert.IsType<DailyResultOutcome.UpstreamFailure>(outcome);
        Assert.Same(cached, failure.Offer);
        Assert.Single(cache.Entries);
    }

    [Fact]
    public async Task UpstreamFailure_WithoutEligibleCache_HasNoOffer()
    {
        var cache = new InMemoryDailyResultCache(TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 0));

        var outcome = await new DailyResultService(FakeNasaNeoClient.Failing(), cache).GetForDateAsync("2026-10-27", CancellationToken.None);

        Assert.Null(Assert.IsType<DailyResultOutcome.UpstreamFailure>(outcome).Offer);
    }

    [Fact]
    public async Task GetLatestCached_ReturnsLatestNonEmptyDayOrNull()
    {
        var cached = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);
        var nasa = FakeNasaNeoClient.Failing();

        Assert.Same(cached, await new DailyResultService(nasa, new InMemoryDailyResultCache(cached)).GetLatestCachedAsync(CancellationToken.None));
        Assert.Null(await new DailyResultService(nasa, new InMemoryDailyResultCache()).GetLatestCachedAsync(CancellationToken.None));
        Assert.Empty(nasa.Requests);
    }

    // Endpoint mapping of the outcomes above (contracts/backend-api.md).

    [Fact]
    public void Endpoint_UpstreamFailureWithOffer_Returns503WithCacheMetadataButNoObservations()
    {
        var cached = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);

        var result = AsteroidEndpoints.ToHttpResult(new DailyResultOutcome.UpstreamFailure(Requested, "HTTP 503", cached));

        var json = Assert.IsType<JsonHttpResult<ApiError>>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, json.StatusCode);
        Assert.Equal(ApiError.UpstreamUnavailableCode, json.Value!.Code);
        Assert.True(json.Value.CachedFallbackAvailable);
        Assert.Equal("2026-10-26", json.Value.CachedDate);

        var serialized = JsonSerializer.Serialize(json.Value, JsonSerializerOptions.Web);
        Assert.DoesNotContain("asteroids", serialized);
        Assert.DoesNotContain("2026-10-26-1", serialized);
    }

    [Fact]
    public void Endpoint_UpstreamFailureWithoutOffer_Returns503WithCacheUnavailable()
    {
        var result = AsteroidEndpoints.ToHttpResult(new DailyResultOutcome.UpstreamFailure(Requested, "HTTP 503", null));

        var json = Assert.IsType<JsonHttpResult<ApiError>>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, json.StatusCode);
        Assert.False(json.Value!.CachedFallbackAvailable);
        Assert.Null(json.Value.CachedDate);
    }

    [Fact]
    public void Endpoint_NoResultsFallback_LabelsRequestedAndDisplayedDatesSeparately()
    {
        var cached = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);
        var response = TestData.Day(Requested, Now, 0);

        var result = AsteroidEndpoints.ToHttpResult(new DailyResultOutcome.NoResultsFallback(Requested, response, cached));

        var body = Assert.IsType<Ok<ApiResponse>>(result).Value!;
        Assert.Equal("2026-10-27", body.RequestedDate);
        Assert.Equal("2026-10-26", body.DisplayedDate);
        Assert.True(body.IsFallback);
        Assert.Equal(ApiResponse.NoResultsReason, body.FallbackReason);
        Assert.Equal(cached.Result.ReceivedAtUtc.UtcDateTime, body.ReceivedAtUtc);
        Assert.Equal(2, body.Asteroids.Count);
    }

    [Fact]
    public void Endpoint_NoResultsNoCache_ReturnsEmptyListWithMessage()
    {
        var result = AsteroidEndpoints.ToHttpResult(new DailyResultOutcome.NoResultsNoCache(Requested, TestData.Day(Requested, Now, 0)));

        var body = Assert.IsType<Ok<ApiResponse>>(result).Value!;
        Assert.Null(body.DisplayedDate);
        Assert.False(body.IsFallback);
        Assert.Empty(body.Asteroids);
        Assert.Equal(AsteroidEndpoints.NoResultsMessage, body.Message);
    }

    [Fact]
    public void Endpoint_LatestCached_ReturnsFallbackForUpstreamUnavailable()
    {
        var cached = TestData.Cached(TestData.Date(2026, 10, 26), TestData.Utc(2026, 10, 26), 2);

        var body = Assert.IsType<Ok<ApiResponse>>(AsteroidEndpoints.ToLatestCachedResult(cached, Requested)).Value!;

        Assert.True(body.IsFallback);
        Assert.Equal(ApiResponse.UpstreamUnavailableReason, body.FallbackReason);
        Assert.Equal("2026-10-27", body.RequestedDate);
        Assert.Equal("2026-10-26", body.DisplayedDate);
    }

    [Fact]
    public void Endpoint_LatestCachedMissing_Returns404()
    {
        var json = Assert.IsType<NotFound<ApiError>>(AsteroidEndpoints.ToLatestCachedResult(null, Requested));

        Assert.Equal(ApiError.CachedResultsUnavailableCode, json.Value!.Code);
    }
}
