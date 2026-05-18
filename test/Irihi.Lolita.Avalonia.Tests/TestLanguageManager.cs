using System.Globalization;

namespace Irihi.Lolita.Avalonia.Tests;

[LolitaManager("./Resources/Strings.resx")]
public partial class TestLanguageManager
{
    public void Reset() => UpdateCulture(CultureInfo.InvariantCulture);
}
