using System.Text.Json.Serialization;

namespace NeoTracker.Core.Models;

/// <summary>
/// Successful asteroid result body, as defined in contracts/backend-api.md.
/// </summary>
public sealed record ApiResponse(
    string? RequestedDate,
    string? DisplayedDate,
    string Source,
    DateTime ReceivedAtUtc,
    bool IsFallback,
    string? FallbackReason,
    IReadOnlyList<AsteroidObservation> Asteroids,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message = null)
{
    public const string NoResultsReason = "no-results";
    public const string UpstreamUnavailableReason = "upstream-unavailable";
}
