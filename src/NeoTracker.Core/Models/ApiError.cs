using System.Text.Json.Serialization;

namespace NeoTracker.Core.Models;

/// <summary>
/// JSON error body with a stable machine-readable code and a user-readable message.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? CachedFallbackAvailable = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CachedDate = null)
{
    public const string InvalidDateCode = "invalid_date";
    public const string UpstreamUnavailableCode = "upstream_unavailable";
    public const string CachedResultsUnavailableCode = "cached_results_unavailable";
}
