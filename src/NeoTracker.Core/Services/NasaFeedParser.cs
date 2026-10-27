using System.Globalization;
using System.Text.Json;
using NeoTracker.Core.Models;

namespace NeoTracker.Core.Services;

/// <summary>
/// Interprets a NASA NeoWs <c>/feed</c> response for a single date. Any payload that
/// does not match the expected shape is rejected with <see cref="NasaFeedFormatException"/>
/// rather than being treated as an empty day.
/// </summary>
public static class NasaFeedParser
{
    public static DailyResult Parse(string json, SelectedDate date, DateTimeOffset receivedAtUtc)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new NasaFeedFormatException("NASA response is not valid JSON.", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new NasaFeedFormatException("NASA response is not a JSON object.");

            var byDate = RequireProperty(root, "near_earth_objects", JsonValueKind.Object);
            var dateKey = date.ToString();

            // NeoWs groups objects by date; a missing key for the requested day means no objects.
            if (!byDate.TryGetProperty(dateKey, out var objects))
                return new DailyResult(date.Value, [], DailyResult.NasaSource, receivedAtUtc);
            if (objects.ValueKind != JsonValueKind.Array)
                throw new NasaFeedFormatException($"near_earth_objects.{dateKey} is not an array.");

            var observations = objects.EnumerateArray().Select(o => ParseObservation(o, dateKey)).ToList();
            return new DailyResult(date.Value, observations, DailyResult.NasaSource, receivedAtUtc);
        }
    }

    private static AsteroidObservation ParseObservation(JsonElement neo, string dateKey)
    {
        if (neo.ValueKind != JsonValueKind.Object)
            throw new NasaFeedFormatException("Near-Earth object entry is not a JSON object.");

        var id = RequireString(neo, "id");
        var name = RequireString(neo, "name");
        var kilometers = RequireProperty(RequireProperty(neo, "estimated_diameter", JsonValueKind.Object), "kilometers", JsonValueKind.Object);
        var hazardous = RequireProperty(neo, "is_potentially_hazardous_asteroid");
        if (hazardous.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new NasaFeedFormatException("is_potentially_hazardous_asteroid is not a boolean.");

        var approach = SelectCloseApproach(RequireProperty(neo, "close_approach_data", JsonValueKind.Array), dateKey);
        var velocity = RequireProperty(approach, "relative_velocity", JsonValueKind.Object);
        var distance = RequireProperty(approach, "miss_distance", JsonValueKind.Object);

        try
        {
            return new AsteroidObservation(
                id,
                name,
                RequireNumber(kilometers, "estimated_diameter_min") * 1000,
                RequireNumber(kilometers, "estimated_diameter_max") * 1000,
                RequireNumber(velocity, "kilometers_per_hour"),
                RequireNumber(distance, "kilometers"),
                hazardous.GetBoolean());
        }
        catch (ArgumentException ex)
        {
            throw new NasaFeedFormatException($"Near-Earth object {id} has invalid values: {ex.Message}", ex);
        }
    }

    private static JsonElement SelectCloseApproach(JsonElement approaches, string dateKey)
    {
        JsonElement? first = null;
        foreach (var approach in approaches.EnumerateArray())
        {
            if (approach.ValueKind != JsonValueKind.Object)
                throw new NasaFeedFormatException("close_approach_data entry is not a JSON object.");
            first ??= approach;
            if (approach.TryGetProperty("close_approach_date", out var approachDate)
                && approachDate.ValueKind == JsonValueKind.String
                && approachDate.GetString() == dateKey)
            {
                return approach;
            }
        }

        return first ?? throw new NasaFeedFormatException("close_approach_data is empty.");
    }

    private static JsonElement RequireProperty(JsonElement element, string name, JsonValueKind? kind = null)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            throw new NasaFeedFormatException($"Required field '{name}' is missing.");
        if (kind is not null && value.ValueKind != kind)
            throw new NasaFeedFormatException($"Field '{name}' is not a JSON {kind}.");
        return value;
    }

    private static string RequireString(JsonElement element, string name)
    {
        var value = RequireProperty(element, name, JsonValueKind.String).GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new NasaFeedFormatException($"Required field '{name}' is empty.");
        return value;
    }

    // NeoWs sends diameters as JSON numbers but velocities and distances as numeric strings.
    private static double RequireNumber(JsonElement element, string name)
    {
        var value = RequireProperty(element, name);
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            return number;
        if (value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            return number;
        throw new NasaFeedFormatException($"Field '{name}' is not a number.");
    }
}

public sealed class NasaFeedFormatException(string message, Exception? innerException = null)
    : FormatException(message, innerException);
