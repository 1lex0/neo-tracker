using System.Globalization;
using System.Text.RegularExpressions;
using NeoTracker.Core.Models;

namespace NeoTracker.Core.Validation;

public enum DateValidationError
{
    None,
    Required,
    InvalidFormat,
    ImpossibleDate,
}

public sealed record SelectedDateValidationResult(SelectedDate? Date, DateValidationError Error, string? Message)
{
    public bool IsValid => Error == DateValidationError.None;
}

/// <summary>
/// Accepts only an exact <c>YYYY-MM-DD</c> string with ASCII digits that names a real
/// Gregorian calendar date. No trimming, alternative separators, or lenient parsing.
/// </summary>
public static partial class SelectedDateValidator
{
    public const string RequiredMessage = "Enter a date in YYYY-MM-DD format.";
    public const string FormatMessage = "Use the YYYY-MM-DD format, for example 2026-10-27.";
    public const string ImpossibleDateMessage = "This date does not exist. Enter a real date in YYYY-MM-DD format.";

    public static SelectedDateValidationResult Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail(DateValidationError.Required, RequiredMessage);

        var match = ExactDatePattern().Match(input);
        if (!match.Success)
            return Fail(DateValidationError.InvalidFormat, FormatMessage);

        var year = int.Parse(match.Groups["year"].ValueSpan, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["month"].ValueSpan, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["day"].ValueSpan, CultureInfo.InvariantCulture);

        if (year < 1 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return Fail(DateValidationError.ImpossibleDate, ImpossibleDateMessage);

        return new SelectedDateValidationResult(new SelectedDate(new DateOnly(year, month, day)), DateValidationError.None, null);
    }

    private static SelectedDateValidationResult Fail(DateValidationError error, string message) => new(null, error, message);

    [GeneratedRegex(@"^(?<year>[0-9]{4})-(?<month>[0-9]{2})-(?<day>[0-9]{2})\z", RegexOptions.CultureInvariant)]
    private static partial Regex ExactDatePattern();
}
