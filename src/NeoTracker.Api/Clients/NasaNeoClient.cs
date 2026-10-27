using Microsoft.Extensions.Options;
using NeoTracker.Api.Configuration;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;
using NeoTracker.Core.Services;

namespace NeoTracker.Api.Clients;

/// <summary>
/// Typed NASA NeoWs HTTP client. Requests exactly one day (<c>start_date</c> equals
/// <c>end_date</c>) and reports every transport, status, or payload problem as
/// <see cref="NasaUnavailableException"/>.
/// </summary>
public sealed class NasaNeoClient(HttpClient httpClient, IOptions<NasaApiOptions> options, TimeProvider timeProvider)
    : INasaNeoClient
{
    public async Task<DailyResult> GetDailyResultAsync(SelectedDate date, CancellationToken cancellationToken)
    {
        var day = date.ToString();
        var requestUri = $"neo/rest/v1/feed?start_date={day}&end_date={day}&api_key={Uri.EscapeDataString(options.Value.ApiKey)}";

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new NasaUnavailableException("NASA NeoWs could not be reached.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NasaUnavailableException("NASA NeoWs did not respond in time.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new NasaUnavailableException($"NASA NeoWs returned HTTP {(int)response.StatusCode}.");

            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new NasaUnavailableException("NASA NeoWs response could not be read.", ex);
            }

            var receivedAtUtc = timeProvider.GetUtcNow();
            try
            {
                return NasaFeedParser.Parse(body, date, receivedAtUtc);
            }
            catch (NasaFeedFormatException ex)
            {
                throw new NasaUnavailableException("NASA NeoWs returned an unreadable response.", ex);
            }
        }
    }
}
