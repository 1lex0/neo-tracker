using Microsoft.Extensions.Options;
using NeoTracker.Api.Configuration;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;

namespace NeoTracker.Api.Clients;

/// <summary>
/// Typed NASA NeoWs HTTP client.
/// </summary>
public sealed class NasaNeoClient(HttpClient httpClient, IOptions<NasaApiOptions> options, TimeProvider timeProvider)
    : INasaNeoClient
{
    public Task<DailyResult> GetDailyResultAsync(SelectedDate date, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Implemented in T014.");
}
