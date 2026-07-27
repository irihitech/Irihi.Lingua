using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

/// <summary>
/// Integration tests for <see cref="ILinguaManager.GetTranslations"/> using
/// the source-generated <see cref="TestLanguageManager"/> backed by real
/// .resx files.
/// </summary>
public class GetTranslationsTests
{
    // The .resx files in Resources/ define:
    //   Invariant: App_Title = "Lingua Headless Test", Greeting_Message = "Hello"
    //   zh-Hans:   App_Title = "Lingua 无头测试",       Greeting_Message = "你好"

    private static LinguaObservableString GetObservable(string key)
    {
        var obs = TestLanguageManager.Instance.GetObservable(key);
        Assert.NotNull(obs);
        return (LinguaObservableString)obs;
    }

    [Fact]
    public void GetTranslations_ReturnsAllCultures()
    {
        var result = TestLanguageManager.Instance.GetTranslations(
            GetObservable("Greeting_Message"));

        Assert.Equal(2, result.Count);
        Assert.Equal("Hello", result[CultureInfo.InvariantCulture]);
        Assert.Equal("你好", result[new CultureInfo("zh-Hans")]);
    }

    [Fact]
    public void GetTranslations_UnknownKey_ReturnsNullObservable()
    {
        var obs = TestLanguageManager.Instance.GetObservable("NonExistent");
        Assert.Null(obs);
    }

    [Fact]
    public void GetTranslations_AllStaticKeysHaveValues()
    {
        foreach (var key in new[] { "App_Title", "Greeting_Message", "Switch_Language", "Format Template" })
        {
            var obs = GetObservable(key);
            var translations = TestLanguageManager.Instance.GetTranslations(obs);

            Assert.NotEmpty(translations);
            Assert.True(translations.ContainsKey(CultureInfo.InvariantCulture),
                $"Key '{key}' missing invariant value");
        }
    }

    [Fact]
    public void GetTranslations_KeyFromObservableMatchesOriginalKey()
    {
        var obs = GetObservable("Switch_Language");
        Assert.Equal("Switch_Language", obs.Key);

        var translations = TestLanguageManager.Instance.GetTranslations(obs);
        Assert.Equal("Switch to Chinese", translations[CultureInfo.InvariantCulture]);
        Assert.Equal("切换到英文", translations[new CultureInfo("zh-Hans")]);
    }

    [Fact]
    public void GetTranslations_SpacedKeyWorks()
    {
        var obs = GetObservable("Format Template");
        Assert.Equal("Format Template", obs.Key);

        var translations = TestLanguageManager.Instance.GetTranslations(obs);
        Assert.Equal("{0}, Page {1}", translations[CultureInfo.InvariantCulture]);
        Assert.Equal("{0}, 第{1}页", translations[new CultureInfo("zh-Hans")]);
    }
}
