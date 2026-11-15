namespace Irihi.Lingua;

/// <summary>
/// An observable string container whose source can be replaced as business state changes.
/// </summary>
/// <remarks>
/// <para>
/// Keep the container instance stable and assign <see cref="Source"/> to select a
/// localized string, a formatted string, or another <see cref="IObservable{T}"/>.
/// Only the selected source is observed. Changing the source unsubscribes the
/// previous one, so subsequent language changes follow the new selection.
/// </para>
/// <para>
/// Sources are subscribed when the first observer arrives and unsubscribed when
/// the last observer leaves. New observers receive the latest value of the active
/// source, including null. Sources such as <see cref="LinguaObservableString"/>
/// emit immediately; other sources may emit later. Until a new source emits,
/// the container does not replay a value from the previous source.
/// </para>
/// <para>
/// Notifications are serialized, including reentrant updates. Like
/// <see cref="LinguaFormatObservable"/>, this container consumes values from
/// long-lived localization streams: source completion and errors are ignored,
/// and do not prevent later source changes. It does not marshal to a UI thread.
/// </para>
/// </remarks>
public sealed class LinguaSwitchObservable : IObservable<string?>
{
    // Monitor permits synchronous source emissions to reenter during Subscribe.
    private readonly object _gate = new();
    private readonly Queue<(ObserverSubscription[] Observers, string? Value)> _notifications = new();
    private IObservable<string?> _source;
    private ObserverSubscription[] _observers = [];
    private SourceObserver? _active;
    private bool _hasValue;
    private string? _value;
    private bool _notifying;

    /// <summary>Creates a container with the initially selected source.</summary>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    public LinguaSwitchObservable(IObservable<string?> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>Gets or replaces the observable selected by the business state.</summary>
    /// <remarks>
    /// Assigning the same source instance does nothing while it is attached.
    /// A synchronous source immediately publishes its current value after a switch.
    /// If subscribing throws, the selected source remains set and the exception
    /// propagates; assigning it again retries the subscription.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The assigned source is null.</exception>
    public IObservable<string?> Source
    {
        get { lock (_gate) return _source; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (_gate)
            {
                if (ReferenceEquals(_source, value) && (_active is not null || _observers.Length == 0))
                    return;

                _source = value;
                DetachCore();
                // Disposing the old source can reenter and attach a newer selection.
                if (_observers.Length > 0 && _active is null)
                    AttachCore();
            }
        }
    }

    /// <summary>
    /// Subscribes to the selected string stream, replaying its latest value if available.
    /// Dispose the returned subscription to stop receiving updates.
    /// </summary>
    public IDisposable Subscribe(IObserver<string?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        var subscription = new ObserverSubscription(this, observer);
        lock (_gate)
        {
            _observers = [.. _observers, subscription];
            try
            {
                if (_active is null)
                    AttachCore();
                else if (_hasValue)
                    NotifyCore([subscription], _value);
            }
            catch
            {
                subscription.Dispose();
                throw;
            }
        }
        return subscription;
    }

    private void AttachCore()
    {
        var active = new SourceObserver(this);
        _active = active;
        try
        {
            // The source can synchronously call back (and even be replaced)
            // before returning its subscription. The observer owns that handle
            // so a handle returned after detachment is still disposed.
            active.SetSubscription(_source.Subscribe(active));
        }
        catch
        {
            if (ReferenceEquals(_active, active))
                DetachCore();
            throw;
        }

        active.Initializing = false;
        if (ReferenceEquals(_active, active) && _hasValue)
            NotifyCore(_observers, _value);
    }

    private void OnValue(SourceObserver source, string? value)
    {
        lock (_gate)
        {
            // Unsubscribing cannot recall an already captured source callback.
            if (!ReferenceEquals(_active, source))
                return;

            _value = value;
            _hasValue = true;
            if (!source.Initializing)
                NotifyCore(_observers, value);
        }
    }

    private void NotifyCore(ObserverSubscription[] observers, string? value)
    {
        _notifications.Enqueue((observers, value));
        if (_notifying)
            return;

        _notifying = true;
        try
        {
            while (_notifications.TryDequeue(out var notification))
                foreach (var observer in notification.Observers)
                    observer.OnNext(notification.Value);
        }
        finally
        {
            _notifying = false;
            _notifications.Clear();
        }
    }

    private void Unsubscribe(ObserverSubscription subscription)
    {
        lock (_gate)
        {
            _observers = _observers.Where(o => !ReferenceEquals(o, subscription)).ToArray();
            if (_observers.Length == 0)
                DetachCore();
        }
    }

    private void DetachCore()
    {
        var active = _active;
        _active = null;
        _hasValue = false;
        _value = null;
        active?.Dispose();
    }

    private sealed class SourceObserver(LinguaSwitchObservable parent) : IObserver<string?>, IDisposable
    {
        private IDisposable? _subscription;
        private bool _disposed;
        public bool Initializing { get; set; } = true;

        // All access is under the parent's gate, including synchronous callbacks.
        public void SetSubscription(IDisposable subscription)
        {
            if (_disposed)
                subscription.Dispose();
            else
                _subscription = subscription;
        }

        public void OnNext(string? value) => parent.OnValue(this, value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void Dispose()
        {
            _disposed = true;
            var subscription = _subscription;
            _subscription = null;
            subscription?.Dispose();
        }
    }

    private sealed class ObserverSubscription(LinguaSwitchObservable parent, IObserver<string?> observer) : IDisposable
    {
        private int _disposed;

        public void OnNext(string? value)
        {
            if (Volatile.Read(ref _disposed) == 0)
                observer.OnNext(value);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                parent.Unsubscribe(this);
        }
    }
}
