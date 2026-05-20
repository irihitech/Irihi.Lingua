using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lolita.Tests;

[TestClass]
public class LolitaKeyTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LolitaKey(null!, new FakeManager()));
    }

    [TestMethod]
    public void Constructor_NullManager_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LolitaKey("App_Title", null!));
    }

    [TestMethod]
    public void Constructor_SetsKey()
    {
        var manager = new FakeManager();
        var key = new LolitaKey("App_Title", manager);
        Assert.AreEqual("App_Title", key.Key);
    }

    [TestMethod]
    public void Constructor_SetsManager()
    {
        var manager = new FakeManager();
        var key = new LolitaKey("App_Title", manager);
        Assert.AreSame(manager, key.Manager);
    }

    // ── Implicit conversion ──────────────────────────────────────────────────

    [TestMethod]
    public void ImplicitConversion_ReturnsKeyString()
    {
        var lolitaKey = new LolitaKey("Greeting", new FakeManager());
        string? result = lolitaKey;
        Assert.AreEqual("Greeting", result);
    }

    [TestMethod]
    public void ImplicitConversion_Null_ReturnsNull()
    {
        LolitaKey? lolitaKey = null;
        string? result = lolitaKey;
        Assert.IsNull(result);
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void ToString_ReturnsKeyString()
    {
        var lolitaKey = new LolitaKey("App_Title", new FakeManager());
        Assert.AreEqual("App_Title", lolitaKey.ToString());
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class FakeManager : ILolitaManager
    {
        public void UpdateCulture(System.Globalization.CultureInfo culture) { }
        public IObservable<string?>? GetObservable(string key) => null;
        public void AddResources(System.Globalization.CultureInfo culture, IReadOnlyDictionary<string, string> resources) { }
    }
}
