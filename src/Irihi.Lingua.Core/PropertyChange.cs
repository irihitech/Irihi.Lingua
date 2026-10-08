using System.ComponentModel;

namespace Irihi.Lingua;

/// <summary>
/// A lightweight reference to an <see cref="INotifyPropertyChanged"/> property
/// that can be passed to <see cref="LinguaFormatExtensions.Format"/> as a live
/// argument.
/// </summary>
/// <remarks>
/// <para>
/// Passing a <see cref="PropertyChange"/> to <c>Format</c> makes the formatted
/// result react to changes of the referenced property: the combiner subscribes
/// the source's <see cref="INotifyPropertyChanged.PropertyChanged"/> event
/// directly and re-reads the property (through <see cref="Getter"/>) on every
/// matching notification.  No observable is created for the property itself.
/// </para>
/// <para>
/// The property is read through the supplied getter delegate instead of
/// reflection or expression trees, which keeps this API trimmer- and
/// NativeAOT-friendly.  Values of value-type properties are boxed once per
/// change, mirroring what any object-typed formatting boundary does.
/// </para>
/// <para>
/// Create instances with the <see cref="PropertyChangeExtensions.Property"/>
/// helper:
/// <code>
/// this.Property(nameof(Page), () =&gt; Page)
/// </code>
/// </para>
/// </remarks>
public sealed class PropertyChange
{
    /// <summary>Gets the object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/>.</summary>
    public INotifyPropertyChanged Source { get; }

    /// <summary>Gets the name of the observed property (e.g. <c>nameof(Page)</c>).</summary>
    public string PropertyName { get; }

    /// <summary>Gets a delegate that reads the current value of the property.</summary>
    public Func<object?> Getter { get; }

    /// <summary>
    /// Initializes a new <see cref="PropertyChange"/> reference.
    /// </summary>
    /// <param name="source">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/>.</param>
    /// <param name="propertyName">The name of the property to observe.</param>
    /// <param name="getter">A delegate that reads the current value of the property.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="getter"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="propertyName"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public PropertyChange(INotifyPropertyChanged source, string propertyName, Func<object?> getter)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        PropertyName = propertyName;
        Getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }
}

/// <summary>
/// Helper for creating <see cref="PropertyChange"/> references.
/// </summary>
public static class PropertyChangeExtensions
{
    /// <summary>
    /// Creates a live-property reference that can be passed to
    /// <see cref="LinguaFormatExtensions.Format"/> as an argument.
    /// </summary>
    /// <param name="source">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/>.</param>
    /// <param name="propertyName">The name of the property to observe (e.g. <c>nameof(Page)</c>).</param>
    /// <param name="getter">A delegate that reads the current value of the property.</param>
    /// <example>
    /// <code>
    /// LanguageManager.Keys.Page_Template.Format(
    ///     this.Property(nameof(Page), () =&gt; Page),
    ///     this.Property(nameof(TotalPages), () =&gt; TotalPages));
    /// </code>
    /// </example>
    public static PropertyChange Property(
        this INotifyPropertyChanged source,
        string propertyName,
        Func<object?> getter) => new(source, propertyName, getter);
}
