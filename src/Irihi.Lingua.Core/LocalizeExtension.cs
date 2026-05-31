using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Irihi.Lingua;

/// <summary>
/// Markup extension that creates a one-way binding to the localized string
/// observable identified by the given <see cref="LinguaKey"/>.
/// </summary>
/// <remarks>
/// <para>
/// The extension retrieves the <see cref="IObservable{T}">IObservable&lt;string&gt;</see>
/// from the key's <see cref="ILinguaManager"/> and converts it into a
/// <see cref="BindingBase"/> via <c>ToBinding()</c>, so that the target property
/// updates automatically whenever the active culture changes.
/// </para>
/// <para>
/// Typical XAML usage:
/// <code><![CDATA[
///   xmlns:lingua="using:Irihi.Lingua"
///   xmlns:local="using:MyApp"
///
///   <TextBlock Text="{lingua:Localize {x:Static local:LanguageManager+Keys.Greeting_Message}}" />
/// ]]></code>
/// </para>
/// </remarks>
public sealed class LocalizeExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the <see cref="LinguaKey"/> that identifies the localized resource.
    /// </summary>
    public LinguaKey? Key { get; set; }

    /// <summary>
    /// Initializes a new instance with no key (supports XAML attribute syntax).
    /// </summary>
    public LocalizeExtension() { }

    /// <summary>
    /// Initializes a new instance with a positional <paramref name="key"/> argument
    /// (supports the compact XAML constructor syntax).
    /// </summary>
    /// <param name="key">The key to bind to.</param>
    public LocalizeExtension(LinguaKey key) => Key = key;

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
