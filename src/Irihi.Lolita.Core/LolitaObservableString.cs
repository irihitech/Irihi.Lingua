using System;
using System.Collections.Generic;

namespace Irihi.Lolita;

/// <summary>
/// A lightweight observable string that behaves like a <c>BehaviorSubject&lt;string?&gt;</c>:
/// it emits the current value immediately upon subscription and notifies all
/// subscribers whenever the value changes.
/// </summary>
/// <remarks>
/// This type is used by the code generated for classes marked with
/// <see cref="LolitaManagerAttribute"/> and does not require any additional
/// reactive library in consumer projects.
/// </remarks>
public sealed class LolitaObservableString : IObservable<string?>
{
    private readonly object _lock = new object();
    private readonly List<IObserver<string?>> _observers = new List<IObserver<string?>>();
    private string? _currentValue;

    /// <summary>
    /// Gets the resource key that this observable string represents.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the most recently emitted value.
    /// </summary>
    public string? CurrentValue
    {
        get
        {
            lock (_lock)
            {
                return _currentValue;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LolitaObservableString"/> with the given key and initial value.
    /// </summary>
    /// <param name="key">The resource key that this observable represents.</param>
    /// <param name="initialValue">The value to emit to new subscribers immediately.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c>.</exception>
    public LolitaObservableString(string key, string? initialValue)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

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
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        string? current;
        lock (_lock)
        {
            _observers.Add(observer);
            current = _currentValue;
        }

        // Emit current value immediately (BehaviorSubject semantics)
        observer.OnNext(current);
        return new Subscription(this, observer);
    }

    /// <summary>
    /// Pushes a new value to all current subscribers.
    /// </summary>
    /// <param name="value">The new value to emit.</param>
    public void OnNext(string? value)
    {
        IObserver<string?>[] observers;
        lock (_lock)
        {
            _currentValue = value;
            observers = _observers.ToArray();
        }

        foreach (var observer in observers)
        {
            observer.OnNext(_currentValue);
        }
    }

    private void Unsubscribe(IObserver<string?> observer)
    {
        lock (_lock)
        {
            _observers.Remove(observer);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private LolitaObservableString? _parent;
        private IObserver<string?>? _observer;

        public Subscription(LolitaObservableString parent, IObserver<string?> observer)
        {
            _parent = parent;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_parent != null && _observer != null)
            {
                _parent.Unsubscribe(_observer);
            }

            _parent = null;
            _observer = null;
        }
    }
}
