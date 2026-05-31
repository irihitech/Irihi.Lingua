using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lingua.Tests;

[TestClass]
public class LinguaManagerAttributeTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NullResourcePath_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new LinguaManagerAttribute(null!));
    }

    [TestMethod]
    public void Constructor_EmptyResourcePath_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new LinguaManagerAttribute(string.Empty));
    }

    [TestMethod]
    public void Constructor_WhitespaceResourcePath_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new LinguaManagerAttribute("   "));
    }

    [TestMethod]
    public void Constructor_ValidResourcePath_SetsResourcePath()
    {
        var attr = new LinguaManagerAttribute("./Resources/Strings.resx");
        Assert.AreEqual("./Resources/Strings.resx", attr.ResourcePath);
    }

    [TestMethod]
    public void Constructor_ValidResourcePath_PreservesPathAsIs()
    {
        const string path = "../some/path/Strings.resx";
        var attr = new LinguaManagerAttribute(path);
        Assert.AreEqual(path, attr.ResourcePath);
    }
}
