using System.Text.Json;
using Microsoft.Extensions.Options;
using NeoTracker.Api.Clients;
using NeoTracker.Api.Configuration;
using NeoTracker.Api.Endpoints;
using NeoTracker.Api.Persistence;
using NeoTracker.Core.Abstractions;
using NeoTracker.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// NASA_API_KEY comes from runtime configuration (environment variable, user secrets,
// command line); the public DEMO_KEY is used only when it is not set.
builder.Services.AddOptions<NasaApiOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        var baseAddress = configuration[$"{NasaApiOptions.SectionName}:BaseAddress"];
        if (!string.IsNullOrWhiteSpace(baseAddress))
            options.BaseAddress = new Uri(baseAddress);
        options.ApiKey = NasaApiOptions.ResolveApiKey(configuration);
    });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDailyResultService, DailyResultService>();

builder.Services.AddSingleton<IDailyResultCache>(services =>
{
    var environment = services.GetRequiredService<IHostEnvironment>();
    var configured = builder.Configuration["Cache:FilePath"];
    var path = string.IsNullOrWhiteSpace(configured) ? Path.Combine("App_Data", "neo-cache.json") : configured;
    return new JsonDailyResultCache(Path.Combine(environment.ContentRootPath, path));
});

builder.Services.AddHttpClient<INasaNeoClient, NasaNeoClient>((services, client) =>
{
    client.BaseAddress = services.GetRequiredService<IOptions<NasaApiOptions>>().Value.BaseAddress;
    client.Timeout = NasaApiOptions.RequestTimeout;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapAsteroidEndpoints();

app.Run();
