using NeoTracker.Core.Models;

namespace NeoTracker.Core.Abstractions;

/// <summary>
/// Fetches one day from NASA NeoWs. Implementations throw
/// <see cref="NasaUnavailableException"/> for transport failures, non-success status
/// codes, and unreadable payloads.
/// </summary>
public interface INasaNeoClient
{
    Task<DailyResult> GetDailyResultAsync(SelectedDate date, CancellationToken cancellationToken);
}

public sealed class NasaUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
