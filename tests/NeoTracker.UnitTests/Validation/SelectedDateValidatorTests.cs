using NeoTracker.Core.Validation;

namespace NeoTracker.UnitTests.Validation;

public class SelectedDateValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyValue_IsRequiredError(string? input)
    {
        var result = SelectedDateValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal(DateValidationError.Required, result.Error);
        Assert.Null(result.Date);
    }

    [Theory]
    [InlineData("2026/10/27")]
    [InlineData("27.10.2026")]
    [InlineData("2026-1-27")]
    [InlineData("2026-10-7")]
    [InlineData("26-10-27")]
    [InlineData("20261027")]
    [InlineData("2026-10-27T00:00")]
    [InlineData(" 2026-10-27")]
    [InlineData("2026-10-27 ")]
    [InlineData("2026-10-27\n")]
    [InlineData("2026 -10-27")]
    [InlineData("yyyy-mm-dd")]
    [InlineData("2026-10-2x")]
    [InlineData("+2026-10-27")]
    [InlineData("٢٠٢٦-١٠-٢٧")] // non-ASCII digits
    public void MalformedValue_IsFormatError(string input)
    {
        var result = SelectedDateValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal(DateValidationError.InvalidFormat, result.Error);
    }

    [Theory]
    [InlineData("2026-02-30")]
    [InlineData("2026-02-31")]
    [InlineData("2025-02-29")] // non-leap year
    [InlineData("1900-02-29")] // century, not divisible by 400
    [InlineData("2026-04-31")]
    [InlineData("2026-13-01")]
    [InlineData("2026-00-10")]
    [InlineData("2026-10-00")]
    [InlineData("2026-10-32")]
    [InlineData("0000-01-01")]
    public void ImpossibleCalendarDate_IsImpossibleDateError(string input)
    {
        var result = SelectedDateValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal(DateValidationError.ImpossibleDate, result.Error);
    }

    [Theory]
    [InlineData("2024-02-29", 2024, 2, 29)] // leap year
    [InlineData("2000-02-29", 2000, 2, 29)] // century divisible by 400
    [InlineData("2026-10-27", 2026, 10, 27)]
    [InlineData("2026-12-31", 2026, 12, 31)]
    [InlineData("0001-01-01", 1, 1, 1)]
    public void RealDate_IsValid(string input, int year, int month, int day)
    {
        var result = SelectedDateValidator.Validate(input);

        Assert.True(result.IsValid);
        Assert.Equal(DateValidationError.None, result.Error);
        Assert.Equal(new DateOnly(year, month, day), result.Date!.Value.Value);
        Assert.Equal(input, result.Date.Value.ToString());
    }

    [Fact]
    public void EveryError_HasUserReadableMessage()
    {
        Assert.All(
            new[] { "", "2026/10/27", "2026-02-30" },
            input => Assert.False(string.IsNullOrWhiteSpace(SelectedDateValidator.Validate(input).Message)));
    }
}
