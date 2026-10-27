namespace NeoTracker.Core.Models;

/// <summary>
/// One near-Earth object from a daily feed, normalized to the units shown in the UI.
/// </summary>
public sealed record AsteroidObservation
{
    public AsteroidObservation(
        string id,
        string name,
        double estimatedDiameterMinMeters,
        double estimatedDiameterMaxMeters,
        double relativeVelocityKph,
        double missDistanceKm,
        bool isPotentiallyHazardous)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Asteroid id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asteroid name is required.", nameof(name));
        RequireNonNegative(estimatedDiameterMinMeters, nameof(estimatedDiameterMinMeters));
        RequireNonNegative(estimatedDiameterMaxMeters, nameof(estimatedDiameterMaxMeters));
        if (estimatedDiameterMaxMeters < estimatedDiameterMinMeters)
            throw new ArgumentException("Maximum diameter must not be less than minimum diameter.", nameof(estimatedDiameterMaxMeters));
        RequireNonNegative(relativeVelocityKph, nameof(relativeVelocityKph));
        RequireNonNegative(missDistanceKm, nameof(missDistanceKm));

        Id = id;
        Name = name;
        EstimatedDiameterMinMeters = estimatedDiameterMinMeters;
        EstimatedDiameterMaxMeters = estimatedDiameterMaxMeters;
        RelativeVelocityKph = relativeVelocityKph;
        MissDistanceKm = missDistanceKm;
        IsPotentiallyHazardous = isPotentiallyHazardous;
    }

    public string Id { get; }
    public string Name { get; }
    public double EstimatedDiameterMinMeters { get; }
    public double EstimatedDiameterMaxMeters { get; }
    public double RelativeVelocityKph { get; }
    public double MissDistanceKm { get; }
    public bool IsPotentiallyHazardous { get; }

    private static void RequireNonNegative(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be a finite non-negative number.");
    }
}
