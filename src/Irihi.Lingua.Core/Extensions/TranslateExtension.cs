using Avalonia;
using Avalonia.Markup.Xaml;

namespace Irihi.Lingua.Extensions;

public sealed class TranslateExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the <see cref="LinguaKey"/> that identifies the localized resource.
    /// </summary>
    public LinguaKey? Key { get; set; }

    /// <summary>
    /// Initializes a new instance with no key (supports XAML attribute syntax).
    /// </summary>
    public TranslateExtension() { }

    /// <summary>
    /// Initializes a new instance with a positional <paramref name="key"/> argument
    /// (supports the compact XAML constructor syntax).
    /// </summary>
    /// <param name="key">The key to bind to.</param>
    public TranslateExtension(LinguaKey key) => Key = key;

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var observable = Key?.Manager.GetObservable(Key.Key);
        if (observable is null) return AvaloniaProperty.UnsetValue;
        return observable.ToBinding();
    }
}
