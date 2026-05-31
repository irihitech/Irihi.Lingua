namespace Irihi.Lingua.AvaloniaDemo;

/// <summary>
/// Demonstrates the LinguaManager pattern in an Avalonia application.
/// The source generator reads the resx files at build time and fills in all
/// observable string properties as well as the <c>UpdateCulture</c> method.
/// </summary>
[LinguaManager("./Resources/Strings.resx")]
public partial class LanguageManager;
