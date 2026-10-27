using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NeoTracker.Api.Clients;
using NeoTracker.Api.Configuration;
using NeoTracker.Core.Abstractions;
using NeoTracker.UnitTests.Fixtures;

namespace NeoTracker.UnitTests.Clients;

public class NasaNeoClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 21, 40, 0, TimeSpan.Zero);

    private static NasaNeoClient CreateClient(TestHttpMessageHandler handler, string apiKey = NasaApiOptions.DemoKey) =>
        new(handler.CreateClient(), Options.Create(new NasaApiOptions { ApiKey = apiKey }), new FakeTimeProvider(Now));

    [Fact]
    public async Task GetDailyResult_RequestsSingleDayWithEncodedKey()
    {
        var handler = TestHttpMessageHandler.Json(FixtureFiles.NasaFeedSample);

        await CreateClient(handler, apiKey: "a b&c").GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None);

        var uri = Assert.Single(handler.Requests).RequestUri!;
        Assert.Equal("/neo/rest/v1/feed", uri.AbsolutePath);
        Assert.Contains("start_date=2026-10-27", uri.Query);
        Assert.Contains("end_date=2026-10-27", uri.Query);
        Assert.Contains("api_key=a%20b%26c", uri.Query);
    }

    [Fact]
    public async Task GetDailyResult_ReturnsParsedDayWithUtcReceiptTime()
    {
        var handler = TestHttpMessageHandler.Json(FixtureFiles.NasaFeedSample);

        var result = await CreateClient(handler).GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None);

        Assert.Equal(3, result.Observations.Count);
        Assert.Equal(Now, result.ReceivedAtUtc);
    }

    [Fact]
    public async Task GetDailyResult_ValidEmptyFeed_ReturnsEmptyDayInsteadOfFailing()
    {
        var handler = TestHttpMessageHandler.Json("""{ "element_count": 0, "near_earth_objects": { "2026-10-27": [] } }""");

        var result = await CreateClient(handler).GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None);

        Assert.True(result.IsEmpty);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetDailyResult_NonSuccessStatus_ThrowsNasaUnavailable(HttpStatusCode status)
    {
        var handler = TestHttpMessageHandler.Json("""{ "error": "nope" }""", status);

        await Assert.ThrowsAsync<NasaUnavailableException>(() =>
            CreateClient(handler).GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None));
    }

    [Theory]
    [InlineData("<html>Gateway error</html>")]
    [InlineData("""{ "element_count": 1 }""")]
    public async Task GetDailyResult_UnreadablePayload_ThrowsNasaUnavailable(string body)
    {
        var handler = TestHttpMessageHandler.Json(body);

        await Assert.ThrowsAsync<NasaUnavailableException>(() =>
            CreateClient(handler).GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None));
    }

    [Fact]
    public async Task GetDailyResult_NetworkFailure_ThrowsNasaUnavailable()
    {
        var handler = TestHttpMessageHandler.Throws(new HttpRequestException("No such host is known."));

        await Assert.ThrowsAsync<NasaUnavailableException>(() =>
            CreateClient(handler).GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None));
    }

    [Fact]
    public async Task GetDailyResult_Timeout_ThrowsNasaUnavailable()
    {
        var handler = TestHttpMessageHandler.Throws(new TaskCanceledException("The request timed out."));

        await Assert.ThrowsAsync<NasaUnavailableException>(() =>
            CreateClient(handler).GetDailyResultAsync(TestData.Date(2026, 10, 27), CancellationToken.None));
    }
}
