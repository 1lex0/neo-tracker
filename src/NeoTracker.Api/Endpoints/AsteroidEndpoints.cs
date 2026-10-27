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

    public static IEndpointRouteBuilder MapAsteroidEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/asteroids");
        group.MapGet("", GetForDateAsync);
        return app;
    }

    public static async Task<IResult> GetForDateAsync(string? date, IDailyResultService service, CancellationToken cancellationToken) =>
        ToHttpResult(await service.GetForDateAsync(date, cancellationToken));

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

        DailyResultOutcome.NoResultsNoCache empty => Results.Ok(new ApiResponse(
            RequestedDate: empty.RequestedDate.ToString(),
            DisplayedDate: null,
            Source: empty.Response.Source,
            ReceivedAtUtc: empty.Response.ReceivedAtUtc.UtcDateTime,
            IsFallback: false,
            FallbackReason: null,
            Asteroids: [],
            Message: NoResultsMessage)),

        DailyResultOutcome.InvalidDate invalid => Results.BadRequest(
            new ApiError(ApiError.InvalidDateCode, invalid.Message)),

        _ => throw new InvalidOperationException($"Unhandled outcome {outcome.GetType().Name}."),
    };
}
