using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Irihi.Lingua.Extensions;

public sealed class FormatTranslateExtension : MarkupExtension
{
    private static readonly FormatTranslateConverter SharedConverter = new();

    /// <summary>
    /// Gets or sets a custom <see cref="IMultiValueConverter"/> that replaces the
    /// default <see cref="FormatTranslateConverter"/>.
    /// </summary>
    /// <remarks>
    /// When this extension builds the <see cref="MultiBinding"/>, the first value
    /// passed to the converter is the format template and the <b>last</b> value is
    /// the owning manager's active <see cref="System.Globalization.CultureInfo"/>.
    /// Custom converters receive that trailing culture value as well.
    /// </remarks>
    public IMultiValueConverter? Converter { get; set; }

    public LinguaKey? FormatKey { get; set; }

    [Content]
    // ReSharper disable once CollectionNeverUpdated.Global
    public IList<TranslateEntry> Items { get; set; } = new List<TranslateEntry>();

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var formatKey = FormatKey;
        if (formatKey is null) return new MultiBinding();

        var formatObservable = formatKey.Manager.GetObservable(formatKey.Key);
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

        // The trailing binding feeds the converter the manager's active culture,
        // so that (a) number/date formatting follows the language selected
        // through the manager instead of the thread culture, and (b) culture
        // switches re-evaluate the MultiBinding.
        bindings.Add(formatKey.Manager.CultureChanges.ToBinding());

        return new MultiBinding
        {
            Converter = Converter ?? SharedConverter,
            Bindings = bindings
        };
    }
}
