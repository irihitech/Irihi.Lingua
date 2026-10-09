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
    /// A custom converter receives the same input shape as before this
    /// extension learned about cultures: the format template first, then one
    /// value per <see cref="TranslateEntry"/>.  Only the built-in converter is
    /// fed the trailing <c>CultureSignal</c> carrying the manager's active
    /// culture; custom converters that want culture awareness can bind the
    /// manager's <c>CultureChanges</c> themselves.
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

        // The trailing binding feeds the built-in converter the manager's active
        // culture (as an internal CultureSignal, distinct from any user-supplied
        // CultureInfo argument), so that number/date formatting follows the
        // language selected through the manager instead of the thread culture,
        // and culture switches re-evaluate the MultiBinding.  Custom converters
        // keep the pre-culture input shape (template + entries).
        var useBuiltinConverter = Converter is null or FormatTranslateConverter;
        if (useBuiltinConverter)
        {
            bindings.Add(new CultureSignalAdapter(formatKey.Manager.CultureChanges).ToBinding());
        }

        return new MultiBinding
        {
            Converter = Converter ?? SharedConverter,
            Bindings = bindings
        };
    }
}
