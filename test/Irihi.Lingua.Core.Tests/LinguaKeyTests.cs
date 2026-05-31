using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lingua.Tests;

[TestClass]
public class LinguaKeyTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LinguaKey(null!, new FakeManager()));
    }

    [TestMethod]
    public void Constructor_NullManager_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LinguaKey("App_Title", null!));
    }

    [TestMethod]
    public void Constructor_SetsKey()
    {
        var manager = new FakeManager();
        var key = new LinguaKey("App_Title", manager);
        Assert.AreEqual("App_Title", key.Key);
    }

    [TestMethod]
    public void Constructor_SetsManager()
    {
        var manager = new FakeManager();
        var key = new LinguaKey("App_Title", manager);
        Assert.AreSame(manager, key.Manager);
    }

    // ── Implicit conversion ──────────────────────────────────────────────────

    [TestMethod]
    public void ImplicitConversion_ReturnsKeyString()
    {
        var linguaKey = new LinguaKey("Greeting", new FakeManager());
        string? result = linguaKey;
        Assert.AreEqual("Greeting", result);
    }

    [TestMethod]
    public void ImplicitConversion_Null_ReturnsNull()
    {
        LinguaKey? linguaKey = null;
        string? result = linguaKey;
        Assert.IsNull(result);
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void ToString_ReturnsKeyString()
    {
        var linguaKey = new LinguaKey("App_Title", new FakeManager());
        Assert.AreEqual("App_Title", linguaKey.ToString());
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class FakeManager : ILinguaManager
    {
        public void UpdateCulture(System.Globalization.CultureInfo culture) { }
        public IObservable<string?>? GetObservable(string key) => null;
        public void AddResources(System.Globalization.CultureInfo culture, IReadOnlyDictionary<string, string> resources) { }
    }
}
