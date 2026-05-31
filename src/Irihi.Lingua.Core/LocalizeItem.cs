using Avalonia.Data;

namespace Irihi.Lingua;

public class LocalizeItem
{
    public LinguaKey? Key { get; set; }
    
    [AssignBinding]
    public BindingBase? Binding { get; set; }
}