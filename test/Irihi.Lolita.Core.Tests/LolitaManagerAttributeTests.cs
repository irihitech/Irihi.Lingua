using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lolita.Tests;

[TestClass]
public class LolitaManagerAttributeTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NullResourcePath_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new LolitaManagerAttribute(null!));
    }

    [TestMethod]
    public void Constructor_EmptyResourcePath_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new LolitaManagerAttribute(string.Empty));
    }

    [TestMethod]
    public void Constructor_WhitespaceResourcePath_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new LolitaManagerAttribute("   "));
    }

    [TestMethod]
    public void Constructor_ValidResourcePath_SetsResourcePath()
    {
        var attr = new LolitaManagerAttribute("./Resources/Strings.resx");
        Assert.AreEqual("./Resources/Strings.resx", attr.ResourcePath);
    }

    [TestMethod]
    public void Constructor_ValidResourcePath_PreservesPathAsIs()
    {
        const string path = "../some/path/Strings.resx";
        var attr = new LolitaManagerAttribute(path);
        Assert.AreEqual(path, attr.ResourcePath);
    }
}
