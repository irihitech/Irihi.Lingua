using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaRuntimeResourcesTests
{
    // ── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_NullCulture_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.Throws<ArgumentNullException>(() =>
            store.Add(null!, new Dictionary<string, string>()));
    }

    [Fact]
    public void Add_NullResources_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.Throws<ArgumentNullException>(() =>
            store.Add(new CultureInfo("en"), null!));
    }

    [Fact]
    public void Add_InvariantCulture_TreatedAsInvariant()
    {
        var store = new LinguaRuntimeResources();
        store.Add(CultureInfo.InvariantCulture, new Dictionary<string, string> { ["Key"] = "Value" });

        var result = store.Resolve(CultureInfo.InvariantCulture);
        Assert.NotNull(result);
        Assert.Equal("Value", result["Key"]);
    }

    [Fact]
    public void Add_SameCultureTwice_MergesEntries()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" });
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["B"] = "updated", ["C"] = "3" });

        var result = store.Resolve(new CultureInfo("en"))!;
        Assert.Equal("1", result["A"]);
        Assert.Equal("updated", result["B"]);
        Assert.Equal("3", result["C"]);
    }

    // ── Resolve ──────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_NullCulture_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.Throws<ArgumentNullException>(() =>
            store.Resolve(null!));
    }

    [Fact]
    public void Resolve_NoEntries_ReturnsNull()
    {
        var store = new LinguaRuntimeResources();
        Assert.Null(store.Resolve(new CultureInfo("en-US")));
    }

    [Fact]
    public void Resolve_ExactCultureMatch()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja-JP"), new Dictionary<string, string> { ["Title"] = "タイトル" });

        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.NotNull(result);
        Assert.Equal("タイトル", result["Title"]);
    }

    [Fact]
    public void Resolve_FallsBackToTwoLetterLanguageName()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja"), new Dictionary<string, string> { ["Title"] = "タイトル" });

        // "ja-JP" is not registered, but "ja" is
        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.NotNull(result);
        Assert.Equal("タイトル", result["Title"]);
    }

    [Fact]
    public void Resolve_ExactMatchTakesPriorityOverTwoLetter()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja"), new Dictionary<string, string> { ["Title"] = "共通" });
        store.Add(new CultureInfo("ja-JP"), new Dictionary<string, string> { ["Title"] = "日本語" });

        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.NotNull(result);
        Assert.Equal("日本語", result["Title"]);
    }

    [Fact]
    public void Resolve_FallsBackToInvariant()
    {
        var store = new LinguaRuntimeResources();
        store.Add(CultureInfo.InvariantCulture, new Dictionary<string, string> { ["Title"] = "Default" });

        // Neither "de-DE" nor "de" is registered — should fall back to invariant
        var result = store.Resolve(new CultureInfo("de-DE"));
        Assert.NotNull(result);
        Assert.Equal("Default", result["Title"]);
    }

    [Fact]
    public void Resolve_UnregisteredCultureWithNoFallback_ReturnsNull()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["K"] = "V" });

        // "ja-JP" / "ja" not registered, and no invariant fallback
        Assert.Null(store.Resolve(new CultureInfo("ja-JP")));
    }

    [Fact]
    public void Resolve_ReturnsSnapshot_MutatingAfterDoesNotAffectResult()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["K"] = "original" });

        var snapshot = store.Resolve(new CultureInfo("en"))!;

        // Add more entries after taking the snapshot
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["K"] = "changed" });

        // Snapshot must be unaffected
        Assert.Equal("original", snapshot["K"]);
    }
}
