namespace NeoTracker.UnitTests.Fixtures;

public static class FixtureFiles
{
    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static string NasaFeedSample => Read("nasa-feed-sample.json");
}
