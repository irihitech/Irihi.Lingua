using System.Globalization;

namespace Irihi.Lingua.Avalonia.Tests;

[LinguaManager("./Resources/Strings.resx")]
public partial class TestLanguageManager
{
    public void Reset() => UpdateCulture(CultureInfo.InvariantCulture);
}
