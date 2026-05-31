using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lingua.Tests;

[TestClass]
public class LinguaRuntimeResourcesTests
{
    // ── Add ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Add_NullCulture_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.ThrowsException<ArgumentNullException>(() =>
            store.Add(null!, new Dictionary<string, string>()));
    }

    [TestMethod]
    public void Add_NullResources_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.ThrowsException<ArgumentNullException>(() =>
            store.Add(new CultureInfo("en"), null!));
    }

    [TestMethod]
    public void Add_InvariantCulture_TreatedAsInvariant()
    {
        var store = new LinguaRuntimeResources();
        store.Add(CultureInfo.InvariantCulture, new Dictionary<string, string> { ["Key"] = "Value" });

        var result = store.Resolve(CultureInfo.InvariantCulture);
        Assert.IsNotNull(result);
        Assert.AreEqual("Value", result["Key"]);
    }

    [TestMethod]
    public void Add_SameCultureTwice_MergesEntries()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" });
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["B"] = "updated", ["C"] = "3" });

        var result = store.Resolve(new CultureInfo("en"))!;
        Assert.AreEqual("1", result["A"]);
        Assert.AreEqual("updated", result["B"]);
        Assert.AreEqual("3", result["C"]);
    }

    // ── Resolve ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Resolve_NullCulture_ThrowsArgumentNullException()
    {
        var store = new LinguaRuntimeResources();
        Assert.ThrowsException<ArgumentNullException>(() =>
            store.Resolve(null!));
    }

    [TestMethod]
    public void Resolve_NoEntries_ReturnsNull()
    {
        var store = new LinguaRuntimeResources();
        Assert.IsNull(store.Resolve(new CultureInfo("en-US")));
    }

    [TestMethod]
    public void Resolve_ExactCultureMatch()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja-JP"), new Dictionary<string, string> { ["Title"] = "タイトル" });

        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.IsNotNull(result);
        Assert.AreEqual("タイトル", result["Title"]);
    }

    [TestMethod]
    public void Resolve_FallsBackToTwoLetterLanguageName()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja"), new Dictionary<string, string> { ["Title"] = "タイトル" });

        // "ja-JP" is not registered, but "ja" is
        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.IsNotNull(result);
        Assert.AreEqual("タイトル", result["Title"]);
    }

    [TestMethod]
    public void Resolve_ExactMatchTakesPriorityOverTwoLetter()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("ja"), new Dictionary<string, string> { ["Title"] = "共通" });
        store.Add(new CultureInfo("ja-JP"), new Dictionary<string, string> { ["Title"] = "日本語" });

        var result = store.Resolve(new CultureInfo("ja-JP"));
        Assert.IsNotNull(result);
        Assert.AreEqual("日本語", result["Title"]);
    }

    [TestMethod]
    public void Resolve_FallsBackToInvariant()
    {
        var store = new LinguaRuntimeResources();
        store.Add(CultureInfo.InvariantCulture, new Dictionary<string, string> { ["Title"] = "Default" });

        // Neither "de-DE" nor "de" is registered — should fall back to invariant
        var result = store.Resolve(new CultureInfo("de-DE"));
        Assert.IsNotNull(result);
        Assert.AreEqual("Default", result["Title"]);
    }

    [TestMethod]
    public void Resolve_UnregisteredCultureWithNoFallback_ReturnsNull()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["K"] = "V" });

        // "ja-JP" / "ja" not registered, and no invariant fallback
        Assert.IsNull(store.Resolve(new CultureInfo("ja-JP")));
    }

    [TestMethod]
    public void Resolve_ReturnsSnapshot_MutatingAfterDoesNotAffectResult()
    {
        var store = new LinguaRuntimeResources();
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["K"] = "original" });

        var snapshot = store.Resolve(new CultureInfo("en"))!;

        // Add more entries after taking the snapshot
        store.Add(new CultureInfo("en"), new Dictionary<string, string> { ["K"] = "changed" });

        // Snapshot must be unaffected
        Assert.AreEqual("original", snapshot["K"]);
    }
}
