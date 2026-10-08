using System.ComponentModel;

namespace Irihi.Lingua;

/// <summary>
/// Bridges <see cref="INotifyPropertyChanged"/> properties into observables,
/// so that plain property changes can feed
/// <see cref="LinguaFormatExtensions.Format"/> and other observable-based APIs
/// without requiring a reactive framework.
/// </summary>
public static class NotifyPropertyChangedExtensions
{
    /// <summary>
    /// Observes the property <paramref name="propertyName"/> of
    /// <paramref name="source"/> and emits its value whenever it changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned observable behaves like a behavior subject: subscribing
    /// immediately emits the current value read via <paramref name="getter"/>,
    /// then every subsequent change of the property.  Values are compared with
    /// <see cref="EqualityComparer{T}.Default"/> and identical consecutive
    /// values are not re-emitted.
    /// </para>
    /// <para>
    /// A <see cref="INotifyPropertyChanged.PropertyChanged"/> event with a
    /// <c>null</c> or empty property name means "something changed" and is
    /// treated as matching every observed property.
    /// </para>
    /// <para>
    /// The property is read through the supplied getter delegate instead of
    /// reflection or expression trees, which keeps this API trimmer- and
    /// NativeAOT-friendly.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="source">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/>.</param>
    /// <param name="propertyName">The name of the property to observe (e.g. <c>nameof(Page)</c>).</param>
    /// <param name="getter">A delegate that reads the current value of the property.</param>
    /// <returns>An <see cref="IObservable{T}"/> with behavior-subject semantics.</returns>
    /// <example>
    /// <code>
    /// this.ObserveProperty(nameof(Page), () =&gt; Page)
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="getter"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="propertyName"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public static IObservable<T?> ObserveProperty<T>(
        this INotifyPropertyChanged source,
        string propertyName,
        Func<T?> getter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(getter);

        return new PropertyChangeObservable<T>(source, propertyName, getter);
    }
}

/// <summary>
/// An observable over a single <see cref="INotifyPropertyChanged"/> property
/// with behavior-subject semantics and reference-counted event wiring.
/// </summary>
internal sealed class PropertyChangeObservable<T> : IObservable<T?>, IBoxedObservable
{
    private readonly INotifyPropertyChanged _source;
    private readonly string _propertyName;
    private readonly Func<T?> _getter;

#if NET9_0_OR_GREATER
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    private volatile IObserver<T?>[] _observers = [];
    private bool _attached;
    private PropertyChangedEventHandler? _handler;
    private T? _lastValue;

    public PropertyChangeObservable(INotifyPropertyChanged source, string propertyName, Func<T?> getter)
    {
        _source = source;
        _propertyName = propertyName;
        _getter = getter;
    }

    public IDisposable Subscribe(IObserver<T?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        T? initial;
        lock (_gate)
        {
            _observers = [.. _observers, observer];
            if (!_attached)
            {
                _attached = true;
                _handler = (_, e) => OnPropertyChanged(e);
                _source.PropertyChanged += _handler;
                _lastValue = _getter();
            }

            initial = _lastValue;
        }

        observer.OnNext(initial);

        return new Subscription(this, observer);
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        // A null/empty property name means "any property may have changed".
        if (!string.IsNullOrEmpty(e.PropertyName) && e.PropertyName != _propertyName)
            return;

        IObserver<T?>[] observers;
        T? value;
        lock (_gate)
        {
            if (!_attached)
                return;

            value = _getter();
            if (EqualityComparer<T?>.Default.Equals(value, _lastValue))
                return;

            _lastValue = value;
            observers = _observers;
        }

        foreach (var observer in observers)
            observer.OnNext(value);
    }

    internal void Unsubscribe(IObserver<T?> observer)
    {
        PropertyChangedEventHandler? handler;

        lock (_gate)
        {
            _observers = _observers.Where(o => !ReferenceEquals(o, observer)).ToArray();
            if (_observers.Length > 0 || !_attached)
                return;

            _attached = false;
            handler = _handler;
            _handler = null;
        }

        if (handler is not null)
            _source.PropertyChanged -= handler;
    }

    IDisposable IBoxedObservable.SubscribeBoxed(IObserver<object?> observer) =>
        Subscribe(new BoxingObserver<T?>(observer));

    private sealed class Subscription(PropertyChangeObservable<T> parent, IObserver<T?> observer) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                parent.Unsubscribe(observer);
        }
    }
}
