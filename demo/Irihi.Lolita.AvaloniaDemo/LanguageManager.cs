using Irihi.Lolita;

namespace Irihi.Lolita.AvaloniaDemo;

/// <summary>
/// Demonstrates the LolitaManager pattern in an Avalonia application.
/// The source generator reads the resx files at build time and fills in all
/// observable string properties as well as the <c>UpdateCulture</c> method.
/// </summary>
[LolitaManager("./Resources/Strings.resx")]
public partial class LanguageManager { }
