using System.Globalization;
using Avalonia;
using Irihi.Lingua.Extensions;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

public class FormatTranslateConverterTests
{
    [Fact]
    public void Convert_WhenNoValuesAreProvided_ReturnsUnsetValue()
    {
        var converter = new FormatTranslateConverter();

        var result = converter.Convert([], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(AvaloniaProperty.UnsetValue, result);
    }

    [Fact]
    public void Convert_WhenFirstValueIsNotAString_ReturnsUnsetValue()
    {
        var converter = new FormatTranslateConverter();

        var result = converter.Convert([123, "ignored"], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(AvaloniaProperty.UnsetValue, result);
    }

    [Fact]
    public void Convert_UsesProvidedCultureForNumericFormatting()
    {
        var converter = new FormatTranslateConverter();

        var result = converter.Convert(["Value: {0:F1}", 1.5], typeof(string), null, new CultureInfo("fr-FR"));

        Assert.Equal("Value: 1,5", result);
    }

    [Fact]
    public void Convert_WhenArgumentIsNull_RendersEmptyPlaceholder()
    {
        var converter = new FormatTranslateConverter();

        var result = converter.Convert(["{0} - Page {1}", "Hello", null], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Hello - Page ", result);
    }

    [Fact]
    public void Convert_WhenFormatStringIsInvalid_ThrowsFormatException()
    {
        var converter = new FormatTranslateConverter();

        Assert.Throws<FormatException>(() =>
            converter.Convert(["{0", "Hello"], typeof(string), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenLastValueIsCultureSignal_UsesItForFormattingAndExcludesItFromArgs()
    {
        var converter = new FormatTranslateConverter();

        // The thread/converter culture is invariant, but the trailing internal
        // signal carries the manager's de-DE culture.
        var result = converter.Convert(
            ["Value: {0:F1}", 1.5, new CultureSignal(new CultureInfo("de-DE"))],
            typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Value: 1,5", result);
    }

    [Fact]
    public void Convert_WhenLastValueIsCultureSignal_AndNoArgs_FormatsWithManagerCulture()
    {
        var converter = new FormatTranslateConverter();

        var result = converter.Convert(
            ["Done {0}", "x", new CultureSignal(new CultureInfo("fr-FR"))],
            typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Done x", result);
    }

    [Fact]
    public void Convert_WhenLastValueIsNotCultureSignal_FallsBackToConverterCulture()
    {
        var converter = new FormatTranslateConverter();

        var result = converter.Convert(
            ["Value: {0:F1}", 1.5],
            typeof(string), null, new CultureInfo("fr-FR"));

        Assert.Equal("Value: 1,5", result);
    }

    [Fact]
    public void Convert_UserSuppliedTrailingCultureInfo_IsFormattedAsArgument()
    {
        // A user-supplied CultureInfo is a regular format argument: only the
        // internal CultureSignal is treated as the culture channel.
        var converter = new FormatTranslateConverter();

        var result = converter.Convert(
            ["{0}", new CultureInfo("ja-JP")],
            typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("ja-JP", result);
    }

    [Fact]
    public void Convert_MixedCultureArgumentsAndSignal_SignalIsConsumed_ArgumentsAreFormatted()
    {
        var converter = new FormatTranslateConverter();
        var cultureArg = new CultureInfo("ja-JP");

        var result = converter.Convert(
            ["{0} | {1:F1}", cultureArg, 1.5, new CultureSignal(new CultureInfo("de-DE"))],
            typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal($"ja-JP | 1,5", result);
    }
}

