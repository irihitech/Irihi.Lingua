namespace Irihi.Lingua.AvaloniaDemo;

/// <summary>
/// Demonstrates the JSON-based LinguaManager pattern in an Avalonia application.
/// Nested JSON objects are flattened into underscore-separated property names:
/// { "app": { "title": "..." } } → app_title.
/// </summary>
[LinguaManager("./Resources/Strings.json")]
public partial class JsonLanguageManager;
