using System;
using System.Globalization;
using Irihi.Lolita.Demo;

Console.WriteLine("=== Irihi Lolita Manager Demo ===\n");

// Subscribe to observable properties – each subscriber receives the
// current value immediately (BehaviorSubject semantics), then any
// future values pushed by UpdateCulture.
using var titleSub = LanguageManager.Instance.App_Title.Subscribe(
    title => Console.WriteLine($"  App Title      : {title}"));

using var greetingSub = LanguageManager.Instance.Greeting_Message.Subscribe(
    msg => Console.WriteLine($"  Greeting       : {msg}"));

// Switch to Simplified Chinese
Console.WriteLine("\n--- UpdateCulture(zh-Hans) ---");
LanguageManager.UpdateCulture(new CultureInfo("zh-Hans"));

// Switch back to the invariant / default culture
Console.WriteLine("\n--- UpdateCulture(InvariantCulture) ---");
LanguageManager.UpdateCulture(CultureInfo.InvariantCulture);

Console.WriteLine("\nDone.");
