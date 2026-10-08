using System.ComponentModel;

namespace Irihi.Lingua;

/// <summary>
/// A fluent builder that combines a localized format template with dynamic
/// arguments into a single observable string — the code-side counterpart of
/// the XAML <c>FormatTranslate</c> markup extension.
/// </summary>
/// <remarks>
/// <para>
/// Create a builder with <see cref="LinguaFormatExtensions.Format(LinguaKey)"/>
/// (or the manager overload for a custom template source), add arguments with
/// the <see cref="Arg(object?)"/> overloads, and finish the chain with
/// <see cref="Build"/>, which composes the final
/// <see cref="IObservable{T}"/> of string.
/// </para>
/// <example>
/// <code>
/// public IObservable&lt;string?&gt; PageText =&gt;
///     LanguageManager.Keys.Page_Template.Format()
///         .Arg(this, nameof(Page), () =&gt; Page)
///         .Arg(this, nameof(TotalPages), () =&gt; TotalPages)
///         .Build();
/// </code>
/// </example>
/// <para>
/// The built observable behaves like a behavior subject: subscribing
/// immediately emits the formatted result computed from the current values,
/// and every subsequent change re-emits a recomputed string.  A recompute is
/// triggered whenever the format template changes (e.g. because the active
/// culture changed), whenever one of the dynamic arguments changes, and — for
/// custom template sources — whenever the manager's active culture changes.
/// Formatting always uses the owning manager's
/// <see cref="ILinguaManager.CurrentCulture"/> rather than the thread culture,
/// so numbers and dates stay consistent with the selected language.
/// </para>
/// <para>
/// The builder is frozen by the first <see cref="Build"/> call; further
/// <c>Arg</c> calls throw.  <see cref="Build"/> may be called repeatedly, each
/// time producing an independent observable with the frozen arguments.
/// </para>
/// </remarks>
public sealed class LinguaFormatBuilder
{
    private readonly ILinguaManager _manager;
    private readonly IObservable<string?> _format;
    private readonly bool _subscribeCultureChanges;
    private readonly List<object?> _slots = [];
    private readonly List<(Func<IObserver<object?>, IDisposable> Subscribe, int Index)> _sources = [];
    private readonly List<(PropertyChange Change, int Index)> _properties = [];

    private bool _built;

    internal LinguaFormatBuilder(
        ILinguaManager manager,
        IObservable<string?> format,
        bool subscribeCultureChanges)
    {
        _manager = manager;
        _format = format;
        _subscribeCultureChanges = subscribeCultureChanges;
    }

    /// <summary>
    /// Adds a constant argument.
    /// </summary>
    /// <param name="value">The constant value (boxed as-is; <c>null</c> is allowed).</param>
    public LinguaFormatBuilder Arg(object? value)
    {
        ThrowIfBuilt();
        _slots.Add(value);
        return this;
    }

    /// <summary>
    /// Adds an observable argument of any element type.  Value-type elements
    /// are boxed on the way through, so observables from other libraries
    /// (e.g. ReactiveUI's <c>WhenAnyValue</c>) work without adaptation.
    /// </summary>
    /// <typeparam name="T">The element type of <paramref name="observable"/>.</typeparam>
    /// <param name="observable">The observable that supplies the argument. Must not be <c>null</c>.</param>
    public LinguaFormatBuilder Arg<T>(IObservable<T> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        ThrowIfBuilt();

        _slots.Add(null);
        _sources.Add((observer => observable.Subscribe(new BoxingObserver<T>(observer)), _slots.Count - 1));
        return this;
    }

    /// <summary>
    /// Adds a live property argument: the built observable subscribes the
    /// source's <see cref="INotifyPropertyChanged.PropertyChanged"/> event
    /// directly and re-reads the property on every matching notification.
    /// </summary>
    /// <remarks>
    /// The property is read through <paramref name="getter"/> instead of
    /// reflection or expression trees, keeping the API trimmer- and
    /// NativeAOT-friendly.  Setting the property to its current value does not
    /// re-emit (identical formatted results are suppressed), and
    /// <c>PropertyChanged</c> events with an empty or <c>null</c> name are
    /// honored as "all properties may have changed".
    /// </remarks>
    /// <param name="source">The object that raises <c>PropertyChanged</c>.</param>
    /// <param name="propertyName">The name of the property to observe (e.g. <c>nameof(Page)</c>).</param>
    /// <param name="getter">A delegate that reads the current value of the property.</param>
    public LinguaFormatBuilder Arg(
        INotifyPropertyChanged source,
        string propertyName,
        Func<object?> getter)
    {
        var change = new PropertyChange(source, propertyName, getter);
        ThrowIfBuilt();

        _slots.Add(null);
        _properties.Add((change, _slots.Count - 1));
        return this;
    }

    /// <summary>
    /// Composes the chain into the final observable string.  The builder is
    /// frozen by this call; <see cref="Build"/> may be called repeatedly, each
    /// time producing an independent observable.
    /// </summary>
    public IObservable<string?> Build()
    {
        _built = true;
        return new LinguaFormatObservable(
            _manager,
            _format,
            _subscribeCultureChanges,
            [.. _slots],
            [.. _sources],
            [.. _properties]);
    }

    private void ThrowIfBuilt()
    {
        if (_built)
            throw new InvalidOperationException(
                "The format builder has already been built and can no longer be modified.");
    }
}
