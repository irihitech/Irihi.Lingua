using Avalonia.Data;

namespace Irihi.Lingua.Extensions;

public class TranslateEntry
{
    public LinguaKey? Key { get; set; }
    
    [AssignBinding]
    public BindingBase? Binding { get; set; }
}