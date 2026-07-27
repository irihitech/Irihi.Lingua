using System.Globalization;

namespace Irihi.Lingua.Avalonia.Tests;

[LinguaManager("./Resources/Strings.resx")]
public partial class TestLanguageManager
{
    public void Reset()
    {
        ClearRuntimeResources();
        UpdateCulture(CultureInfo.InvariantCulture);
    }
}
