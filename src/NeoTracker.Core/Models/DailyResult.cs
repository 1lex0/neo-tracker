using System.Text.Json.Serialization;

namespace NeoTracker.Core.Models;

/// <summary>
/// The normalized result for one date. A successful empty result is valid but is never
/// eligible as a fallback display.
/// </summary>
public sealed record DailyResult
{
    public const string NasaSource = "NASA NeoWs API";

    public DailyResult(DateOnly date, IReadOnlyList<AsteroidObservation> observations, string source, DateTimeOffset receivedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));
        if (receivedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Receipt time must be expressed in UTC.", nameof(receivedAtUtc));

        Date = date;
        Observations = observations;
        Source = source;
        ReceivedAtUtc = receivedAtUtc;
    }

    public DateOnly Date { get; }
    public IReadOnlyList<AsteroidObservation> Observations { get; }
    public string Source { get; }
    public DateTimeOffset ReceivedAtUtc { get; }

    [JsonIgnore]
    public bool IsEmpty => Observations.Count == 0;
}
