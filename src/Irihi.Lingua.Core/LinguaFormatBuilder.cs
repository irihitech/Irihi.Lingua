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
/// the <see cref="Arg(object?)"/> overloads, and subscribe directly: the
/// builder itself implements <see cref="IObservable{T}"/>, so no terminal
/// method is needed.
/// </para>
/// <example>
/// <code>
/// public IObservable&lt;string?&gt; PageText =&gt;
///     LanguageManager.Keys.Page_Template.Format()
///         .Arg(this, nameof(Page), () =&gt; Page)
///         .Arg(this, nameof(TotalPages), () =&gt; TotalPages);
/// </code>
/// </example>
/// <para>
/// The builder behaves like a behavior subject: subscribing immediately emits
/// the formatted result computed from the current values, and every subsequent
/// change re-emits a recomputed string.  A recompute is triggered whenever the
/// format template changes (e.g. because the active culture changed), whenever
/// one of the dynamic arguments changes, and — for custom template sources —
/// whenever the manager's active culture changes.  Formatting always uses the
/// owning manager's <see cref="ILinguaManager.CurrentCulture"/> rather than
/// the thread culture, so numbers and dates stay consistent with the selected
/// language.
/// </para>
/// <para>
/// The underlying combiner is created lazily on the first subscription and
/// shared by all subscribers; from that point on the builder is frozen and
/// further <c>Arg</c> calls throw.
/// </para>
/// </remarks>
public sealed class LinguaFormatBuilder : IObservable<string?>
{
    private readonly ILinguaManager _manager;
    private readonly IObservable<string?> _format;
    private readonly bool _subscribeCultureChanges;
    private readonly List<object?> _slots = [];
    private readonly List<(Func<IObserver<object?>, IDisposable> Subscribe, int Index)> _sources = [];
    private readonly List<(PropertyChange Change, int Index)> _properties = [];

#if NET9_0_OR_GREATER
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    private LinguaFormatObservable? _combiner;
    private bool _frozen;

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
        ThrowIfFrozen();
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
        ThrowIfFrozen();

        _slots.Add(null);
        _sources.Add((observer => observable.Subscribe(new BoxingObserver<T>(observer)), _slots.Count - 1));
        return this;
    }

    /// <summary>
    /// Adds a live property argument: the combiner subscribes the source's
    /// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
    /// event directly and re-reads the property on every matching notification.
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
        System.ComponentModel.INotifyPropertyChanged source,
        string propertyName,
        Func<object?> getter)
    {
        var change = new PropertyChange(source, propertyName, getter);
        ThrowIfFrozen();

        _slots.Add(null);
        _properties.Add((change, _slots.Count - 1));
        return this;
    }

    /// <summary>
    /// Subscribes an observer.  The underlying combiner is created lazily on
    /// the first subscription and shared by all subscribers; the observer
    /// immediately receives the formatted result computed from the current
    /// values, then every recomputed value.
    /// </summary>
    public IDisposable Subscribe(IObserver<string?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        LinguaFormatObservable combiner;
        lock (_gate)
        {
            _frozen = true;
            combiner = _combiner ??= new LinguaFormatObservable(
                _manager,
                _format,
                _subscribeCultureChanges,
                [.. _slots],
                [.. _sources],
                [.. _properties]);
        }

        return combiner.Subscribe(observer);
    }

    private void ThrowIfFrozen()
    {
        if (_frozen)
            throw new InvalidOperationException(
                "The format builder has already been subscribed and can no longer be modified.");
    }
}
