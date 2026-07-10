using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaKeyTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LinguaKey(null!, new FakeManager()));
    }

    [Fact]
    public void Constructor_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LinguaKey("App_Title", null!));
    }

    [Fact]
    public void Constructor_SetsKey()
    {
        var manager = new FakeManager();
        var key = new LinguaKey("App_Title", manager);
        Assert.Equal("App_Title", key.Key);
    }

    [Fact]
    public void Constructor_SetsManager()
    {
        var manager = new FakeManager();
        var key = new LinguaKey("App_Title", manager);
        Assert.Same(manager, key.Manager);
    }

    // ── Implicit conversion ──────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ReturnsKeyString()
    {
        var linguaKey = new LinguaKey("Greeting", new FakeManager());
        string? result = linguaKey;
        Assert.Equal("Greeting", result);
    }

    [Fact]
    public void ImplicitConversion_Null_ReturnsNull()
    {
        LinguaKey? linguaKey = null;
        string? result = linguaKey;
        Assert.Null(result);
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsKeyString()
    {
        var linguaKey = new LinguaKey("App_Title", new FakeManager());
        Assert.Equal("App_Title", linguaKey.ToString());
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class FakeManager : ILinguaManager
    {
        public System.Globalization.CultureInfo CurrentCulture => System.Globalization.CultureInfo.InvariantCulture;
        public IObservable<System.Globalization.CultureInfo> CultureChanges => null!;
        public void UpdateCulture(System.Globalization.CultureInfo culture) { }
        public IObservable<string?>? GetObservable(string key) => null;
        public void AddResources(System.Globalization.CultureInfo culture, IReadOnlyDictionary<string, string> resources) { }
    }
}
