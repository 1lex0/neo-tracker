using System.Globalization;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;

namespace NeoTracker.Api.Endpoints;

/// <summary>
/// HTTP surface described in contracts/backend-api.md. Handlers only translate
/// business outcomes into status codes and JSON bodies.
/// </summary>
public static class AsteroidEndpoints
{
    public const string NoResultsMessage = "No asteroid results or cached non-empty results are available.";
    public const string UpstreamUnavailableMessage = "NASA data is currently unavailable.";
    public const string CachedResultsUnavailableMessage = "No cached non-empty asteroid results are available.";

    public static IEndpointRouteBuilder MapAsteroidEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/asteroids");
        group.MapGet("", GetForDateAsync);
        group.MapGet("/cache/latest", GetLatestCachedAsync);
        return app;
    }

    public static async Task<IResult> GetForDateAsync(string? date, IDailyResultService service, CancellationToken cancellationToken) =>
        ToHttpResult(await service.GetForDateAsync(date, cancellationToken));

    /// <summary>
    /// Called only after the user chooses the cache offer. <paramref name="requestedDate"/>
    /// is optional and only echoed back so the UI can label it next to the cached date.
    /// </summary>
    public static async Task<IResult> GetLatestCachedAsync(string? requestedDate, IDailyResultService service, CancellationToken cancellationToken)
    {
        SelectedDate? requested = DateOnly.TryParseExact(requestedDate, SelectedDate.Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? new SelectedDate(parsed)
            : null;
        return ToLatestCachedResult(await service.GetLatestCachedAsync(cancellationToken), requested);
    }

    public static IResult ToHttpResult(DailyResultOutcome outcome) => outcome switch
    {
        DailyResultOutcome.Fresh fresh => Results.Ok(new ApiResponse(
            RequestedDate: fresh.RequestedDate.ToString(),
            DisplayedDate: fresh.RequestedDate.ToString(),
            Source: fresh.Result.Source,
            ReceivedAtUtc: fresh.Result.ReceivedAtUtc.UtcDateTime,
            IsFallback: false,
            FallbackReason: null,
            Asteroids: fresh.Result.Observations)),

        DailyResultOutcome.NoResultsFallback fallback => Results.Ok(
            ToFallbackResponse(fallback.Fallback, fallback.RequestedDate, ApiResponse.NoResultsReason)),

        DailyResultOutcome.NoResultsNoCache empty => Results.Ok(new ApiResponse(
            RequestedDate: empty.RequestedDate.ToString(),
            DisplayedDate: null,
            Source: empty.Response.Source,
            ReceivedAtUtc: empty.Response.ReceivedAtUtc.UtcDateTime,
            IsFallback: false,
            FallbackReason: null,
            Asteroids: [],
            Message: NoResultsMessage)),

        // Cached observations are deliberately not part of this response: the user must
        // first choose to view them through /api/asteroids/cache/latest.
        DailyResultOutcome.UpstreamFailure failure => Results.Json(
            new ApiError(
                ApiError.UpstreamUnavailableCode,
                UpstreamUnavailableMessage,
                CachedFallbackAvailable: failure.Offer is not null,
                CachedDate: failure.Offer is null ? null : new SelectedDate(failure.Offer.Result.Date).ToString()),
            statusCode: StatusCodes.Status503ServiceUnavailable),

        DailyResultOutcome.InvalidDate invalid => Results.BadRequest(
            new ApiError(ApiError.InvalidDateCode, invalid.Message)),

        _ => throw new InvalidOperationException($"Unhandled outcome {outcome.GetType().Name}."),
    };

    public static IResult ToLatestCachedResult(CachedDailyResult? cached, SelectedDate? requestedDate) =>
        cached is null
            ? Results.NotFound(new ApiError(ApiError.CachedResultsUnavailableCode, CachedResultsUnavailableMessage))
            : Results.Ok(ToFallbackResponse(cached, requestedDate, ApiResponse.UpstreamUnavailableReason));

    // A fallback carries the cached day's own date and receipt time, never the request's,
    // so it cannot be mistaken for a fresh response for the requested date.
    private static ApiResponse ToFallbackResponse(CachedDailyResult cached, SelectedDate? requestedDate, string reason) => new(
        RequestedDate: requestedDate?.ToString(),
        DisplayedDate: new SelectedDate(cached.Result.Date).ToString(),
        Source: cached.Result.Source,
        ReceivedAtUtc: cached.Result.ReceivedAtUtc.UtcDateTime,
        IsFallback: true,
        FallbackReason: reason,
        Asteroids: cached.Result.Observations);
}
