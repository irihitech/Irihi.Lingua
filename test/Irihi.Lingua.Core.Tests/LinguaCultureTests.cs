using System.ComponentModel;
using System.Globalization;
using Irihi.Lingua;
using Xunit;

namespace Irihi.Lingua.Core.Tests;

public class LinguaCultureTests
{
    [Fact]
    public void DefaultCulture_IsInvariantCulture()
    {
        var lc = new LinguaCulture();
        Assert.Same(CultureInfo.InvariantCulture, lc.Culture);
    }

    [Fact]
    public void CultureName_SetsCulture()
    {
        var lc = new LinguaCulture { CultureName = "zh-Hans" };
        Assert.Equal("zh-Hans", lc.Culture.Name);
    }

    [Fact]
    public void CultureName_EmptyString_YieldsInvariantCulture()
    {
        var lc = new LinguaCulture { CultureName = "" };
        Assert.Same(CultureInfo.InvariantCulture, lc.Culture);
    }

    [Fact]
    public void CultureName_Get_ReturnsCultureName()
    {
        var lc = new LinguaCulture { Culture = new CultureInfo("ja-JP") };
        Assert.Equal("ja-JP", lc.CultureName);
    }

    [Fact]
    public void DisplayText_UsesDisplayName_WhenSet()
    {
        var lc = new LinguaCulture { Culture = new CultureInfo("en"), DisplayName = "English" };
        Assert.Equal("English", lc.DisplayText);
    }

    [Fact]
    public void DisplayText_FallsBackToNativeName()
    {
        var lc = new LinguaCulture { Culture = new CultureInfo("zh-Hans") };
        Assert.Equal(new CultureInfo("zh-Hans").NativeName, lc.DisplayText);
    }

    [Fact]
    public void InvariantCulture_StaticMember_HasCorrectCulture()
    {
        Assert.Same(CultureInfo.InvariantCulture, LinguaCulture.InvariantCulture.Culture);
        Assert.Equal(CultureInfo.InvariantCulture.NativeName, LinguaCulture.InvariantCulture.DisplayName);
    }

    // ── TypeConverter ────────────────────────────────────────────────────────

    [Fact]
    public void TypeConverter_CanConvertFrom_String()
    {
        var converter = new LinguaCultureTypeConverter();
        Assert.True(converter.CanConvertFrom(typeof(string)));
    }

    [Fact]
    public void TypeConverter_CannotConvertFrom_Int()
    {
        var converter = new LinguaCultureTypeConverter();
        Assert.False(converter.CanConvertFrom(typeof(int)));
    }

    [Fact]
    public void TypeConverter_ConvertFrom_ReturnsLinguaCulture()
    {
        var converter = new LinguaCultureTypeConverter();
        var result = converter.ConvertFrom("ja-JP");
        Assert.IsType<LinguaCulture>(result);
        Assert.Equal("ja-JP", ((LinguaCulture)result!).Culture.Name);
    }
}
