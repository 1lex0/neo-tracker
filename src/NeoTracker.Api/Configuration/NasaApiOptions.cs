namespace NeoTracker.Api.Configuration;

/// <summary>
/// Server-side NASA NeoWs settings. The API key never leaves the backend.
/// </summary>
public sealed class NasaApiOptions
{
    public const string SectionName = "Nasa";

    /// <summary>Runtime configuration key (usually an environment variable) that overrides the default key.</summary>
    public const string ApiKeyVariable = "NASA_API_KEY";

    /// <summary>NASA's publicly documented demo key; allowed as a default by constitution v1.1.0.</summary>
    public const string DemoKey = "DEMO_KEY";

    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public Uri BaseAddress { get; set; } = new("https://api.nasa.gov/");

    public string ApiKey { get; set; } = DemoKey;

    public static string ResolveApiKey(IConfiguration configuration)
    {
        var configured = configuration[ApiKeyVariable];
        return string.IsNullOrWhiteSpace(configured) ? DemoKey : configured.Trim();
    }
}
