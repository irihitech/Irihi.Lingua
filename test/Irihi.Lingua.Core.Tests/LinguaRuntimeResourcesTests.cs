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
    public void Resolve_FallsBackToParentCulture()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja"), new Dictionary<string, string> { ["Title"] = "タイトル" });

        // "ja-JP" is not registered, but its parent "ja" is
        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.NotNull(result);
        Assert.Equal("タイトル", result["Title"]);
    }

    [Fact]
    public void Resolve_ExactMatchTakesPriorityOverParent()
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
    public void Resolve_RegionalCultureFallsBackToNeutralParent()
    {
        // 核心场景：zh-CN → zh-Hans → zh → invariant
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("zh-Hans"), new Dictionary<string, string> { ["Title"] = "简体中文" });
        store.Add(CultureInfo.InvariantCulture, new Dictionary<string, string> { ["Title"] = "Default" });

        // "zh-CN" should resolve through its parent chain: zh-CN → zh-Hans
        var result = store.Resolve(new CultureInfo("zh-CN"));
        Assert.NotNull(result);
        Assert.Equal("简体中文", result["Title"]);
    }

    [Fact]
    public void Resolve_WalksFullParentChain()
    {
        // zh-CN → zh-Hans → zh → invariant — stop at first match (zh)
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("zh"), new Dictionary<string, string> { ["Title"] = "中文" });
        store.Add(new CultureInfo("zh-Hans"), new Dictionary<string, string> { ["Title"] = "简体中文" });
        store.Add(CultureInfo.InvariantCulture, new Dictionary<string, string> { ["Title"] = "Default" });

        // "zh-CN" → first parent match is "zh-Hans" (not "zh")
        var result = store.Resolve(new CultureInfo("zh-CN"));
        Assert.NotNull(result);
        Assert.Equal("简体中文", result["Title"]);
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

    // ── GetAllValuesForKey ─────────────────────────────────────────────────────

    [Fact]
    public void GetAllValuesForKey_NullKey_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.Throws<ArgumentNullException>(() =>
            store.GetAllValuesForKey(null!).ToList());
    }

    [Fact]
    public void GetAllValuesForKey_EmptyStore_ReturnsEmpty()
    {
        var store = new LinguaRuntimeResources();
        var result = store.GetAllValuesForKey("AnyKey");
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllValuesForKey_KeyNotFound_ReturnsEmpty()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["A"] = "1" });

        var result = store.GetAllValuesForKey("NonExistent");
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllValuesForKey_SingleCulture_ReturnsCorrectPair()
    {
        var store = new LinguaRuntimeResources();
        var enUS = new CultureInfo("en-US");
        store.Add(enUS, new Dictionary<string, string> { ["Title"] = "Hello" });

        var result = store.GetAllValuesForKey("Title").ToList();
        Assert.Single(result);
        Assert.Equal(enUS, result[0].Key);
        Assert.Equal("Hello", result[0].Value);
    }

    [Fact]
    public void GetAllValuesForKey_MultipleCultures_ReturnsAllMatches()
    {
        var store = new LinguaRuntimeResources();
        var enUS = new CultureInfo("en-US");
        var jaJP = new CultureInfo("ja-JP");
        var invariant = CultureInfo.InvariantCulture;

        store.Add(enUS, new Dictionary<string, string> { ["Title"] = "Hello" });
        store.Add(jaJP, new Dictionary<string, string> { ["Title"] = "こんにちは" });
        store.Add(invariant, new Dictionary<string, string> { ["Title"] = "Default" });

        var result = store.GetAllValuesForKey("Title").ToList();
        Assert.Equal(3, result.Count);

        var dict = result.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal("Hello", dict[enUS]);
        Assert.Equal("こんにちは", dict[jaJP]);
        Assert.Equal("Default", dict[invariant]);
    }

    [Fact]
    public void GetAllValuesForKey_ReturnsSnapshot_NotAffectedBySubsequentAdd()
    {
        var store = new LinguaRuntimeResources();
        var enUS = new CultureInfo("en-US");
        store.Add(enUS, new Dictionary<string, string> { ["Title"] = "original" });

        var snapshot = store.GetAllValuesForKey("Title").ToList();

        // Add more entries after taking the snapshot
        store.Add(new CultureInfo("ja-JP"), new Dictionary<string, string> { ["Title"] = "後で追加" });

        // Snapshot must still have only the original entry
        Assert.Single(snapshot);
        Assert.Equal(enUS, snapshot[0].Key);
        Assert.Equal("original", snapshot[0].Value);
    }

    // ── Safety: the Parent-chain walk must never loop infinitely ─────────────

    [Fact]
    public void Resolve_InvariantCulture_DoesNotLoopInfinitely()
    {
        // InvariantCulture.Parent == InvariantCulture — must exit immediately.
        var store = new LinguaRuntimeResources();
        var result = store.Resolve(CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_CultureWithNoStoreEntries_DoesNotLoopInfinitely()
    {
        // A culture that traces all the way up to invariant (nothing in store)
        // must complete quickly thanks to the for-loop depth guard.
        var store = new LinguaRuntimeResources();
        var result = store.Resolve(new CultureInfo("zh-CN"));
        Assert.Null(result);
    }
}
