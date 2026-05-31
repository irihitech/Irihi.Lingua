using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Irihi.Lingua.Extensions;

public sealed class FormatTranslateExtension : MarkupExtension
{
    private static readonly FormatTranslateConverter SharedConverter = new();
    public IMultiValueConverter? Converter { get; set; }

    public LinguaKey? FormatKey { get; set; }

    [Content]
    // ReSharper disable once CollectionNeverUpdated.Global
    public IList<TranslateEntry> Items { get; set; } = new List<TranslateEntry>();

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var formatObservable = FormatKey?.Manager.GetObservable(FormatKey.Key);
        
        if (formatObservable is null) return new MultiBinding();

        var bindings = new List<BindingBase> { formatObservable.ToBinding() };

        foreach (var item in Items)
        {
            if (item.Key is not null)
            {
                var observable = item.Key.Manager.GetObservable(item.Key.Key);
                if (observable is not null) bindings.Add(observable.ToBinding());
                else bindings.Add(new Binding { Source = null });
            }
            else if (item.Binding is not null)
            {
                bindings.Add(item.Binding);
            }
            else
            {
                bindings.Add(new Binding { Source = null });
            }
        }

        return new MultiBinding
        {
            Converter = Converter ?? SharedConverter,
            Bindings = bindings
        };
    }
}