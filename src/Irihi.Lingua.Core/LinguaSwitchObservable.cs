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
    // This gate protects state only. Never call upstream or downstream code
    // while holding it: a formatted source has its own notification gate.
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
            IDisposable? previous;
            lock (_gate)
            {
                if (ReferenceEquals(_source, value) && (_active is not null || _observers.Length == 0))
                    return;

                _source = value;
                previous = DetachCore();
            }

            previous?.Dispose();
            // Disposal can reenter and select another source. Attach only the
            // current selection, and only if nobody has already attached it.
            AttachCurrent();
            DrainNotifications();
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
            // During attachment, the final priming broadcast includes this
            // subscriber. Replaying here would deliver the initial value twice.
            if (_active is { Initializing: false } && _hasValue)
                _notifications.Enqueue(([subscription], _value));
        }
        try
        {
            AttachCurrent();
            DrainNotifications();
        }
        catch
        {
            subscription.Dispose();
            throw;
        }
        return subscription;
    }

    private void AttachCurrent()
    {
        SourceObserver active;
        IObservable<string?> source;
        lock (_gate)
        {
            if (_active is not null || _observers.Length == 0)
                return;

            source = _source;
            active = new SourceObserver(this);
            _active = active;
        }

        IDisposable subscription;
        try
        {
            subscription = source.Subscribe(active);
        }
        catch
        {
            lock (_gate)
            {
                if (ReferenceEquals(_active, active))
                    DetachCore();
            }
            throw;
        }

        lock (_gate)
        {
            if (ReferenceEquals(_active, active))
            {
                active.Subscription = subscription;
                active.Initializing = false;
                if (_hasValue)
                    _notifications.Enqueue((_observers, _value));
                return;
            }
        }

        // The source was replaced or the last observer left before Subscribe
        // returned. Dispose its late handle without touching the new selection.
        subscription.Dispose();
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
                _notifications.Enqueue((_observers, value));
        }
        DrainNotifications();
    }

    private void DrainNotifications()
    {
        lock (_gate)
        {
            if (_notifying || _notifications.Count == 0)
                return;
            _notifying = true;
        }

        try
        {
            while (true)
            {
                (ObserverSubscription[] Observers, string? Value) notification;
                lock (_gate)
                {
                    if (!_notifications.TryDequeue(out notification))
                    {
                        _notifying = false;
                        return;
                    }
                }
                foreach (var observer in notification.Observers)
                    observer.OnNext(notification.Value);
            }
        }
        catch
        {
            lock (_gate)
            {
                _notifying = false;
                _notifications.Clear();
            }
            throw;
        }
    }

    private void Unsubscribe(ObserverSubscription subscription)
    {
        IDisposable? previous = null;
        lock (_gate)
        {
            _observers = _observers.Where(o => !ReferenceEquals(o, subscription)).ToArray();
            if (_observers.Length == 0)
                previous = DetachCore();
        }
        previous?.Dispose();
    }

    // Invalidate before releasing the gate, then let the caller dispose outside it.
    private IDisposable? DetachCore()
    {
        var subscription = _active?.Subscription;
        if (_active is not null)
            _active.Subscription = null;
        _active = null;
        _hasValue = false;
        _value = null;
        return subscription;
    }

    private sealed class SourceObserver(LinguaSwitchObservable parent) : IObserver<string?>
    {
        // Accessed only while holding the parent's state gate.
        public IDisposable? Subscription { get; set; }
        public bool Initializing { get; set; } = true;

        public void OnNext(string? value) => parent.OnValue(this, value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
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
