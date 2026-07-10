namespace Irihi.Lingua;

/// <summary>
/// A lightweight observable that behaves like a <c>BehaviorSubject&lt;T&gt;</c>:
/// it emits the current value immediately upon subscription and notifies all
/// subscribers whenever the value changes.
/// </summary>
/// <typeparam name="T">The type of value emitted to observers.</typeparam>
/// <remarks>
/// This type is the foundation used by <see cref="LinguaObservableString"/> and
/// by the code generated for classes marked with <see cref="LinguaManagerAttribute"/>.
/// It does not require any additional reactive library.
/// </remarks>
public class LinguaObservable<T> : IObservable<T>
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private volatile IObserver<T>[] _observers = [];
    private T _currentValue;

    /// <summary>
    /// Gets the resource key that this observable represents.
    /// </summary>
    /// <remarks>
    /// For observables created without a resource key (e.g. via
    /// <see cref="LinguaObservableString.FromLiteral"/>), this is
    /// <see cref="string.Empty"/>.
    /// </remarks>
    public string Key { get; }

    /// <summary>
    /// Gets the most recently emitted value.
    /// </summary>
    public T CurrentValue => _currentValue;

    /// <summary>
    /// Initializes a new instance of <see cref="LinguaObservable{T}"/> with the
    /// given key and initial value.
    /// </summary>
    /// <param name="key">
    /// The resource key that this observable represents.  Must not be <c>null</c>.
    /// </param>
    /// <param name="initialValue">The value to emit to new subscribers immediately.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c>.</exception>
    public LinguaObservable(string key, T initialValue)
    {
        ArgumentNullException.ThrowIfNull(key);

        Key = key;
        _currentValue = initialValue;
    }

    /// <summary>
    /// Subscribes an observer.  The observer receives the current value immediately,
    /// then subsequent values as they are emitted via <see cref="OnNext"/>.
    /// </summary>
    /// <param name="observer">The observer to subscribe.</param>
    /// <returns>An <see cref="IDisposable"/> that cancels the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="observer"/> is <c>null</c>.</exception>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        T current;
        lock (_lock)
        {
            _observers = [.. _observers, observer];
            current = _currentValue;
        }
        observer.OnNext(current);
        return new Subscription<T>(this, observer);
    }

    /// <summary>
    /// Pushes a new value to all current subscribers.
    /// </summary>
    /// <param name="value">The new value to emit.</param>
    public void OnNext(T value)
    {
        var observers = _observers;
        _currentValue = value;

        foreach (var observer in observers)
            observer.OnNext(value);
    }

    internal void Unsubscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            _observers = _observers.Where(o => !ReferenceEquals(o, observer)).ToArray();
        }
    }
}

sealed class Subscription<T>(LinguaObservable<T> parent, IObserver<T> observer) : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            parent.Unsubscribe(observer);
    }
}
