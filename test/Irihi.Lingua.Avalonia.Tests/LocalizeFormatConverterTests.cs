using System.Globalization;
using Avalonia;
using Irihi.Lingua.Extensions;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

public class LocalizeFormatConverterTests
{
    [Fact]
    public void Convert_WhenNoValuesAreProvided_ReturnsUnsetValue()
    {
        var converter = new LocalizeFormatConverter();

        var result = converter.Convert([], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(AvaloniaProperty.UnsetValue, result);
    }

    [Fact]
    public void Convert_WhenFirstValueIsNotAString_ReturnsUnsetValue()
    {
        var converter = new LocalizeFormatConverter();

        var result = converter.Convert([123, "ignored"], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(AvaloniaProperty.UnsetValue, result);
    }

    [Fact]
    public void Convert_UsesProvidedCultureForNumericFormatting()
    {
        var converter = new LocalizeFormatConverter();

        var result = converter.Convert(["Value: {0:F1}", 1.5], typeof(string), null, new CultureInfo("fr-FR"));

        Assert.Equal("Value: 1,5", result);
    }

    [Fact]
    public void Convert_WhenArgumentIsNull_RendersEmptyPlaceholder()
    {
        var converter = new LocalizeFormatConverter();

        var result = converter.Convert(["{0} - Page {1}", "Hello", null], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Hello - Page ", result);
    }

    [Fact]
    public void Convert_WhenFormatStringIsInvalid_ThrowsFormatException()
    {
        var converter = new LocalizeFormatConverter();

        Assert.Throws<FormatException>(() =>
            converter.Convert(["{0", "Hello"], typeof(string), null, CultureInfo.InvariantCulture));
    }
}

