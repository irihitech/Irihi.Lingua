using System.Globalization;

namespace Irihi.Lolita.Avalonia.Tests;

/// <summary>
/// Hand-written <see cref="ILolitaManager"/> that mirrors the structure the
/// source generator would produce for a class annotated with
/// <c>[LolitaManager]</c>.  Used in headless tests so the tests have no
/// dependency on the generator or on real .resx files.
/// </summary>
/// <remarks>
/// The outer-type static field <see cref="Instance"/> is fully initialised
/// before the nested <c>Keys</c> type is constructed, so the circular-looking
/// references in <c>Keys</c> are safe (C# static-init ordering guarantee).
/// </remarks>
public sealed class TestLanguageManager : ILolitaManager
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    /// <summary>The singleton instance exposed to consumers.</summary>
    public static readonly TestLanguageManager Instance = new();

    // ── Observable backing fields ─────────────────────────────────────────────

    private readonly LolitaObservableString _appTitle        = new("App_Title",        "Lolita Headless Test");
    private readonly LolitaObservableString _greetingMessage = new("Greeting_Message", "Hello");
    private readonly LolitaObservableString _switchLanguage  = new("Switch_Language",  "Switch to Chinese");

    // ── Public observable properties (mirrors generated output) ───────────────

    /// <summary>Observable application title.</summary>
    public IObservable<string?> App_Title        => _appTitle;

    /// <summary>Observable greeting message.</summary>
    public IObservable<string?> Greeting_Message => _greetingMessage;

    /// <summary>Observable switch-language label.</summary>
    public IObservable<string?> Switch_Language  => _switchLanguage;

    // ── Nested Keys class (same pattern as generated code) ────────────────────

    /// <summary>
    /// Typed resource keys — static members here mirror the
    /// <c>public static readonly LolitaKey</c> fields the generator emits.
    /// </summary>
    public static class Keys
    {
        /// <summary>Key for the application title resource.</summary>
        public static readonly LolitaKey App_Title        = new("App_Title",        Instance);

        /// <summary>Key for the greeting message resource.</summary>
        public static readonly LolitaKey Greeting_Message = new("Greeting_Message", Instance);

        /// <summary>Key for the switch-language label resource.</summary>
        public static readonly LolitaKey Switch_Language  = new("Switch_Language",  Instance);
    }

    // ── Private constructor (singleton) ───────────────────────────────────────

    private TestLanguageManager() { }

    // ── ILolitaManager ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void UpdateCulture(CultureInfo? culture)
    {
        bool isChinese = string.Equals(
            culture?.TwoLetterISOLanguageName, "zh",
            StringComparison.OrdinalIgnoreCase);

        _appTitle.OnNext(isChinese        ? "Lolita 无头测试"  : "Lolita Headless Test");
        _greetingMessage.OnNext(isChinese ? "你好"            : "Hello");
        _switchLanguage.OnNext(isChinese  ? "切换到英文"       : "Switch to Chinese");
    }

    /// <inheritdoc/>
    public IObservable<string?>? GetObservable(string key) => key switch
    {
        "App_Title"        => _appTitle,
        "Greeting_Message" => _greetingMessage,
        "Switch_Language"  => _switchLanguage,
        _                  => null
    };

    // ── Test helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Resets all observable strings to their default (English / invariant)
    /// values.  Call this at the start of each test to guarantee isolation
    /// between tests that share the singleton.
    /// </summary>
    public void Reset() => UpdateCulture(CultureInfo.InvariantCulture);
}
