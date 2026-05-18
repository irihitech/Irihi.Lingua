using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Irihi.Lolita;

/// <summary>
/// Markup extension that creates a one-way binding to the localized string
/// observable identified by the given <see cref="LolitaKey"/>.
/// </summary>
/// <remarks>
/// <para>
/// The extension retrieves the <see cref="IObservable{T}">IObservable&lt;string&gt;</see>
/// from the key's <see cref="ILolitaManager"/> and converts it into a
/// <see cref="BindingBase"/> via <c>ToBinding()</c>, so that the target property
/// updates automatically whenever the active culture changes.
/// </para>
/// <para>
/// Typical XAML usage:
/// <code><![CDATA[
///   xmlns:lolita="using:Irihi.Lolita"
///   xmlns:local="using:MyApp"
///
///   <TextBlock Text="{lolita:Localize {x:Static local:LanguageManager+Keys.Greeting_Message}}" />
/// ]]></code>
/// </para>
/// </remarks>
public sealed class LocalizeExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the <see cref="LolitaKey"/> that identifies the localized resource.
    /// </summary>
    public LolitaKey? Key { get; set; }

    /// <summary>
    /// Initializes a new instance with no key (supports XAML attribute syntax).
    /// </summary>
    public LocalizeExtension() { }

    /// <summary>
    /// Initializes a new instance with a positional <paramref name="key"/> argument
    /// (supports the compact XAML constructor syntax).
    /// </summary>
    /// <param name="key">The key to bind to.</param>
    public LocalizeExtension(LolitaKey key) => Key = key;

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Key is null)
            return AvaloniaProperty.UnsetValue;

        var observable = Key.Manager.GetObservable(Key.Key);
        if (observable is null)
            return AvaloniaProperty.UnsetValue;

        return observable.ToBinding();
    }
}
