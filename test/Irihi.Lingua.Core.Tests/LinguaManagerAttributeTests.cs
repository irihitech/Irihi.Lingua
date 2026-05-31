using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaManagerAttributeTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullResourcePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LinguaManagerAttribute(null!));
    }

    [Fact]
    public void Constructor_EmptyResourcePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LinguaManagerAttribute(string.Empty));
    }

    [Fact]
    public void Constructor_WhitespaceResourcePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LinguaManagerAttribute("   "));
    }

    [Fact]
    public void Constructor_ValidResourcePath_SetsResourcePath()
    {
        var attr = new LinguaManagerAttribute("./Resources/Strings.resx");
        Assert.Equal("./Resources/Strings.resx", attr.ResourcePath);
    }

    [Fact]
    public void Constructor_ValidResourcePath_PreservesPathAsIs()
    {
        const string path = "../some/path/Strings.resx";
        var attr = new LinguaManagerAttribute(path);
        Assert.Equal(path, attr.ResourcePath);
    }
}
