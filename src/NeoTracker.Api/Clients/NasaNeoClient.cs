using System.Net;
using Microsoft.Extensions.Options;
using NeoTracker.Api.Configuration;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Models;
using NeoTracker.Core.Services;

namespace NeoTracker.Api.Clients;

/// <summary>
/// Typed NASA NeoWs HTTP client. Requests exactly one day (<c>start_date</c> equals
/// <c>end_date</c>) and reports every transport, status, or payload problem as
/// <see cref="NasaUnavailableException"/>. Log entries never include the request URI,
/// because it carries the API key.
/// </summary>
public sealed class NasaNeoClient(
    HttpClient httpClient,
    IOptions<NasaApiOptions> options,
    TimeProvider timeProvider,
    ILogger<NasaNeoClient> logger)
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
            logger.LogWarning("NASA NeoWs transport failure for {Date}: {HttpRequestError} {ErrorMessage}", day, ex.HttpRequestError, ex.Message);
            throw new NasaUnavailableException("NASA NeoWs could not be reached.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("NASA NeoWs request for {Date} timed out after {Timeout}", day, httpClient.Timeout);
            throw new NasaUnavailableException("NASA NeoWs did not respond in time.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("NASA NeoWs returned unsuccessful status {StatusCode} for {Date}", (int)response.StatusCode, day);
                throw new NasaUnavailableException(response.StatusCode == HttpStatusCode.TooManyRequests
                    ? "NASA NeoWs rate limit exceeded."
                    : $"NASA NeoWs returned HTTP {(int)response.StatusCode}.");
            }

            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning("NASA NeoWs response body for {Date} could not be read: {ErrorMessage}", day, ex.Message);
                throw new NasaUnavailableException("NASA NeoWs response could not be read.", ex);
            }

            var receivedAtUtc = timeProvider.GetUtcNow();
            try
            {
                return NasaFeedParser.Parse(body, date, receivedAtUtc);
            }
            catch (NasaFeedFormatException ex)
            {
                logger.LogWarning(ex, "NASA NeoWs response for {Date} could not be parsed", day);
                throw new NasaUnavailableException("NASA NeoWs returned an unreadable response.", ex);
            }
        }
    }
}
