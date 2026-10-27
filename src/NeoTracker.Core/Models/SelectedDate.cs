using System.Globalization;

namespace NeoTracker.Core.Models;

/// <summary>
/// A real Gregorian calendar date requested by the user. Its canonical text form is
/// exactly <c>YYYY-MM-DD</c>.
/// </summary>
public readonly record struct SelectedDate(DateOnly Value)
{
    public const string Format = "yyyy-MM-dd";

    public override string ToString() => Value.ToString(Format, CultureInfo.InvariantCulture);
}
