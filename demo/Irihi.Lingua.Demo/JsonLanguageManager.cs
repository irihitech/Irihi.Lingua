namespace Irihi.Lingua.Demo;

/// <summary>
/// Demonstrates the JSON-based LinguaManager pattern.
/// Nested JSON objects are flattened into underscore-separated property names:
/// { "app": { "title": "..." } } → app_title.
/// </summary>
[LinguaManager("./Resources/Strings.json")]
public partial class JsonLanguageManager;
