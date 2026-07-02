namespace Irihi.Lingua;

/// <summary>
/// A lightweight observable string that behaves like a <c>BehaviorSubject&lt;string?&gt;</c>:
/// it emits the current value immediately upon subscription and notifies all
/// subscribers whenever the value changes.
/// </summary>
/// <remarks>
/// This type is used by the code generated for classes marked with
/// <see cref="LinguaManagerAttribute"/> and does not require any additional
/// reactive library in consumer projects.
/// </remarks>
public sealed class LinguaObservableString : IObservable<string?>
{
    #if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
    #else
    private readonly object _lock = new();
    #endif
    private volatile IObserver<string?>[] _observers = [];
    private string? _currentValue;

    /// <summary>
    /// Gets the resource key that this observable string represents.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the most recently emitted value.
    /// </summary>
    public string? CurrentValue => Volatile.Read(ref _currentValue);

    /// <summary>
    /// Creates an <see cref="LinguaObservableString"/> from a literal string value,
    /// without tying it to any resource (.resx) key or <see cref="ILinguaManager"/>.
    /// </summary>
    /// <remarks>
    /// The returned observable immediately emits <paramref name="value"/> to new
    /// subscribers (behaviour-subject semantics) and never notifies again unless
    /// <see cref="OnNext"/> is called explicitly.
    /// This is useful when you need an <see cref="IObservable{String}"/> but the
    /// source is a hard-coded string rather than a localized resource — for example,
    /// exposing a constant label alongside localized observables in a ViewModel.
    /// </remarks>
    /// <param name="value">
    /// The string value to wrap.  A <c>null</c> value is allowed and results in an
    /// observable that emits <c>null</c> to subscribers.
    /// </param>
    /// <returns>
    /// A new <see cref="LinguaObservableString"/> whose <see cref="Key"/> is an
    /// empty string (since no resource key is involved).
    /// </returns>
    public static LinguaObservableString FromLiteral(string? value) =>
        new(string.Empty, value);

    /// <summary>
    /// Initializes a new instance of <see cref="LinguaObservableString"/> with the given key and initial value.
    /// </summary>
    /// <param name="key">The resource key that this observable represents.</param>
    /// <param name="initialValue">The value to emit to new subscribers immediately.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c>.</exception>
    public LinguaObservableString(string key, string? initialValue)
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
    public IDisposable Subscribe(IObserver<string?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        string? current;
        lock (_lock)
        {
            _observers = [.. _observers, observer];  // atomic array replace
            current = _currentValue;
        }
        observer.OnNext(current);
        return new Subscription(this, observer);
    }

    /// <summary>
    /// Pushes a new value to all current subscribers.
    /// </summary>
    /// <param name="value">The new value to emit.</param>
    public void OnNext(string? value)
    {
        var observers = _observers;
        // Always update value, but avoid lock when no one is listening
        Volatile.Write(ref _currentValue, value);

        foreach (var observer in observers)
            observer.OnNext(value);
    }

    internal void Unsubscribe(IObserver<string?> observer)
    {
        lock (_lock)
        {
            _observers = _observers.Where(o => o != observer).ToArray();
        }
    }
}

sealed class Subscription(LinguaObservableString parent, IObserver<string?> observer) : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            parent.Unsubscribe(observer);
    }
}