using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Irihi.Lingua;

public class LocalizeFormatExtension: MarkupExtension
{
    public LinguaKey? FormatKey { get; set; }
    
    [Content]
    public IList<LocalizeItem> Items { get; set; } = new List<LocalizeItem>();
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var bindings = new List<BindingBase>();
        bindings.Add(FormatKey?.Manager.GetObservable(FormatKey.Key)?.ToBinding());
        foreach (LocalizeItem item in Items)
        {
            if (item.Key is not null)
            {
                 bindings.Add(item.Key.Manager.GetObservable(item.Key.Key)?.ToBinding());
            }
            else if (item.Binding is not null)
            {
                bindings.Add(item.Binding);
            }
        }
        return new MultiBinding()
        {
            Converter = new LocalizeFormatConverter(),
            Bindings = bindings,
        };
    }
}