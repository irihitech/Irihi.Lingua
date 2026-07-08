using System.Globalization;
using Irihi.Lingua.Demo;

Console.WriteLine("=== Irihi Lingua Manager Demo ===\n");

// ── RESX-based manager ──────────────────────────────────────────────────
Console.WriteLine("── RESX Manager ──");

using var titleSub = LanguageManager.Instance.App_Title.Subscribe(
    title => Console.WriteLine($"  App Title      : {title}"));

using var greetingSub = LanguageManager.Instance.Greeting_Message.Subscribe(
    msg => Console.WriteLine($"  Greeting       : {msg}"));

// Switch to Simplified Chinese
Console.WriteLine("\n--- UpdateCulture(zh-Hans) ---");
LanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

// Switch back to the invariant / default culture
Console.WriteLine("\n--- UpdateCulture(InvariantCulture) ---");
LanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);

// ── JSON-based manager ──────────────────────────────────────────────────
Console.WriteLine("\n── JSON Manager ──");

// Nested JSON keys are flattened: { "app": { "title": "..." } } → app_title
using var jsonTitleSub = JsonLanguageManager.Instance.app_title.Subscribe(
    title => Console.WriteLine($"  App Title      : {title}"));

using var jsonGreetingSub = JsonLanguageManager.Instance.app_greeting.Subscribe(
    msg => Console.WriteLine($"  Greeting       : {msg}"));

Console.WriteLine("\n--- UpdateCulture(zh-Hans) ---");
JsonLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

Console.WriteLine("\n--- UpdateCulture(InvariantCulture) ---");
JsonLanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);

Console.WriteLine("\nDone.");
