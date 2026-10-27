using System.Net;
using System.Text;

namespace NeoTracker.UnitTests.Fixtures;

/// <summary>
/// Replaces the network for <see cref="HttpClient"/> so tests run offline and can
/// inspect every request that would have been sent.
/// </summary>
public sealed class TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public static TestHttpMessageHandler Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

    public static TestHttpMessageHandler Throws(Exception exception) =>
        new(_ => throw exception);

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("https://api.nasa.gov/") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(respond(request));
    }
}
