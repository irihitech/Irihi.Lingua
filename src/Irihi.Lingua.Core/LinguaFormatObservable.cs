using System.ComponentModel;
using System.Globalization;

namespace Irihi.Lingua;

/// <summary>
/// Combines a localized format-template observable, zero or more argument
/// sources and a manager's culture stream into a single observable string.
/// </summary>
/// <remarks>
/// <para>
/// This is the code-side counterpart of the XAML <c>FormatTranslate</c> markup
/// extension.  It behaves like a behavior subject: subscribing immediately
/// emits the formatted result computed from the current values, and every
/// subsequent change re-emits a recomputed string.  A recompute is triggered
/// by any of the following events:
/// </para>
/// <list type="bullet">
///   <item>The format-template observable pushes a new template.</item>
///   <item>Any dynamic argument observable pushes a new value.</item>
///   <item>The owning manager's <see cref="ILinguaManager.CultureChanges"/>
///   pushes a new culture, even if the selected resource variant omits the
///   template key and does not push a new template.</item>
/// </list>
/// <para>
/// Formatting always uses <see cref="ILinguaManager.CurrentCulture"/> of the
/// owning manager, never the current thread culture, so that formatted values
/// stay consistent with the language selected through the manager.
/// </para>
/// <para>
/// Source subscriptions are reference-counted: the sources are subscribed when
/// the first observer subscribes and unsubscribed when the last observer
/// disposes, so an unused combined observable does not keep itself alive via
/// long-lived manager observables.
/// </para>
/// <para>
/// Notifications are serialized. Updates and subscriptions initiated from an
/// observer callback are queued until the current notification completes.
/// </para>
/// </remarks>
public sealed class LinguaFormatObservable : IObservable<string?>
{
    private readonly ILinguaManager _manager;
    private readonly IObservable<string?> _format;
    private readonly object?[] _currentArgs;
    private readonly (Func<IObserver<object?>, IDisposable> Subscribe, int Index)[] _sourceArgs;
    private readonly (PropertyChange Change, int Index)[] _propertyArgs;
    private readonly (Func<object?> Read, int Index)[] _computedArgs;

    // Monitor rather than System.Threading.Lock: attaching subscribes sources
    // while holding the gate, and the resulting same-thread synchronous
    // callbacks must be able to reenter it.
    private readonly object _gate = new();
    private readonly Queue<(FormatSubscription[] Observers, string Value)> _notifications = new();

    private FormatSubscription[] _observers = [];
    private bool _attached;
    private bool _attaching;
    private bool _notifying;
    private string? _currentFormat;
    private string? _lastEmitted;
    private IDisposable? _formatSubscription;
    private IDisposable? _cultureSubscription;
    private IDisposable[] _argSubscriptions = [];
    private PropertyChangedEventHandler? _propertyHandler;

    /// <summary>
    /// Initializes the combined observable with pre-classified arguments.
    /// Constants sit in <paramref name="initialArgs"/> at their positions;
    /// observable, live-property and computed arguments are supplied separately
    /// with their argument indices.  Creation is internal — use a
    /// <see cref="LinguaFormatBuilder"/> chain ending in
    /// <see cref="LinguaFormatBuilder.Build"/>.
    /// </summary>
    internal LinguaFormatObservable(
        ILinguaManager manager,
        IObservable<string?> format,
        object?[] initialArgs,
        (Func<IObserver<object?>, IDisposable> Subscribe, int Index)[] sources,
        (PropertyChange Change, int Index)[] properties,
        (Func<object?> Read, int Index)[] computedArgs)
    {
        _manager = manager;
        _format = format;
        _currentArgs = initialArgs;
        _sourceArgs = sources;
        _propertyArgs = properties;
        _computedArgs = computedArgs;
    }

    /// <summary>
    /// Subscribes an observer.  The observer immediately receives the result
    /// formatted from the current template, arguments and culture, then every
    /// recomputed value.
    /// </summary>
    /// <remarks>
    /// Subscriptions made from an observer callback receive their initial value
    /// after the current notification completes.
    /// </remarks>
    public IDisposable Subscribe(IObserver<string?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        var subscription = new FormatSubscription(this, observer);

        lock (_gate)
        {
            _observers = [.. _observers, subscription];
            if (!_attached)
            {
                // Attach runs while holding the gate so concurrent subscribers
                // cannot observe half-initialized snapshots.  Source callbacks
                // fire synchronously inside Subscribe and reenter the gate
                // (Monitor is reentrant on the same thread); cross-thread
                // pushes block until attachment completes, so their emit
                // decision is made after the initial emission.  The
                // _attaching flag only suppresses the same-thread priming
                // emissions so the initial value is sent exactly once.
                _attached = true;
                _attaching = true;
                try
                {
                    Attach();
                }
                catch
                {
                    // A throwing source subscription or getter must not leave
                    // the combiner half-attached: roll the state back so the
                    // observer is not retained, partial source subscriptions
                    // are disposed, and a later Subscribe can retry.
                    DetachSources();
                    _attached = false;
                    _observers = RemoveOne(_observers, subscription);
                    throw;
                }
                finally
                {
                    _attaching = false;
                }
            }

            // Serialize the initial value with all subsequent notifications.
            _lastEmitted = ComputeCore();
            try
            {
                NotifyCore([subscription], _lastEmitted);
            }
            catch
            {
                subscription.Dispose();
                throw;
            }
        }

        return subscription;
    }

    private void Attach()
    {
        _formatSubscription = _format.Subscribe(new FormatObserver(this));

        _argSubscriptions = new IDisposable[_sourceArgs.Length];
        for (var i = 0; i < _sourceArgs.Length; i++)
        {
            _argSubscriptions[i] = _sourceArgs[i].Subscribe(new ArgObserver(this, _sourceArgs[i].Index));
        }

        _cultureSubscription = _manager.CultureChanges.Subscribe(new CultureObserver(this));

        if (_propertyArgs.Length > 0 || _computedArgs.Length > 0)
        {
            // Wire events before reading snapshots so a concurrent change
            // cannot fall between the initial read and event registration.
            // The caller holds the gate; callbacks from another thread wait
            // until the initial emission has completed.
            if (_propertyArgs.Length > 0)
            {
                _propertyHandler = OnPropertyChanged;
                foreach (var source in _propertyArgs.Select(p => p.Change.Source)
                             .Distinct<INotifyPropertyChanged>(ReferenceEqualityComparer.Instance))
                    source.PropertyChanged += _propertyHandler;
            }

            foreach (var (change, index) in _propertyArgs)
                _currentArgs[index] = change.Getter();
            foreach (var (read, index) in _computedArgs)
                _currentArgs[index] = read();
        }
    }

    private void OnFormatValue(string? value)
    {
        lock (_gate)
        {
            _currentFormat = value;
            EmitIfChangedCore();
        }
    }

    private void OnArgValue(int index, object? value)
    {
        lock (_gate)
        {
            _currentArgs[index] = value;
            EmitIfChangedCore();
        }
    }

    private void OnCultureChanged()
    {
        lock (_gate)
        {
            EmitIfChangedCore();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A null/empty property name means "any property may have changed".
        var wildcard = string.IsNullOrEmpty(e.PropertyName);

        lock (_gate)
        {
            foreach (var (change, index) in _propertyArgs)
            {
                if (!ReferenceEquals(sender, change.Source) ||
                    (!wildcard && e.PropertyName != change.PropertyName))
                    continue;

                _currentArgs[index] = change.Getter();
            }

            EmitIfChangedCore();
        }
    }

    // The caller holds _gate through snapshot updates, computation and delivery.
    private void EmitIfChangedCore()
    {
        if (_attaching || _observers.Length == 0)
            return;

        var result = ComputeCore();
        // A culture switch can push both the culture and the template. Suppress
        // identical results, as well as argument updates that leave text unchanged.
        if (result == _lastEmitted)
            return;

        _lastEmitted = result;
        NotifyCore(_observers, result);
    }

    private void NotifyCore(FormatSubscription[] observers, string value)
    {
        if (observers.Length == 0)
            return;

        _notifications.Enqueue((observers, value));
        if (_notifying)
            return;

        // Monitor blocks concurrent pushes. The queue also handles same-thread
        // reentrancy: finish notifying every observer before sending the next value.
        _notifying = true;
        try
        {
            while (_notifications.TryDequeue(out var notification))
            {
                foreach (var observer in notification.Observers)
                    observer.OnNext(notification.Value);
            }
        }
        finally
        {
            _notifying = false;
            _notifications.Clear();
        }
    }

    /// <summary>
    /// Forces a refresh: re-reads every pull-based argument (live-property
    /// getters and computed getters), recomputes the formatted string with the
    /// manager's active culture, and notifies current subscribers even when
    /// the result is unchanged.
    /// </summary>
    /// <remarks>
    /// Observable arguments are push-only — <c>Refresh</c> does not (and
    /// cannot) re-read them; their values only arrive through their own
    /// notifications.  Constants are fixed at build time.  Calling
    /// <c>Refresh</c> while nobody is subscribed is safe: the snapshots are
    /// still refreshed and the next subscriber receives the up-to-date value.
    /// </remarks>
    public void Refresh()
    {
        lock (_gate)
        {
            foreach (var (change, index) in _propertyArgs)
                _currentArgs[index] = change.Getter();
            foreach (var (read, index) in _computedArgs)
                _currentArgs[index] = read();

            var result = ComputeCore();
            _lastEmitted = result;
            NotifyCore(_observers, result);
        }
    }

    private string ComputeCore()
    {
        var format = _currentFormat;
        if (string.IsNullOrEmpty(format))
            return string.Empty;

        try
        {
            return string.Format(_manager.CurrentCulture, format, _currentArgs);
        }
        catch (FormatException)
        {
            // Malformed template — surface the raw template instead of
            // throwing inside the observer pipeline.
            return format;
        }
    }

    private void Unsubscribe(FormatSubscription observer)
    {
        lock (_gate)
        {
            // Remove only one occurrence: subscribing the same observer twice
            // creates two independent registrations, and disposing either one
            // must not cancel the other.
            _observers = RemoveOne(_observers, observer);
            if (_observers.Length > 0 || !_attached)
                return;

            // Detach under the gate, symmetric with Attach: releasing the lock
            // first would let a concurrent Subscribe re-attach and overwrite
            // the subscription fields before this thread disposes them
            // (killing the new subscriptions and leaking the old ones).
            _attached = false;
            DetachSources();
        }
    }

    /// <summary>Disposes every source subscription and unwires event handlers.</summary>
    private void DetachSources()
    {
        _formatSubscription?.Dispose();
        _formatSubscription = null;
        _cultureSubscription?.Dispose();
        _cultureSubscription = null;
        foreach (var subscription in _argSubscriptions)
            subscription?.Dispose();
        _argSubscriptions = [];

        if (_propertyHandler is not null)
        {
            foreach (var source in _propertyArgs.Select(p => p.Change.Source)
                         .Distinct<INotifyPropertyChanged>(ReferenceEqualityComparer.Instance))
                source.PropertyChanged -= _propertyHandler;
            _propertyHandler = null;
        }
    }

    /// <summary>Returns a copy of <paramref name="observers"/> without the first occurrence of <paramref name="observer"/>.</summary>
    private static FormatSubscription[] RemoveOne(FormatSubscription[] observers, FormatSubscription observer)
    {
        var index = Array.FindIndex(observers, o => ReferenceEquals(o, observer));
        if (index < 0)
            return observers;

        var result = new FormatSubscription[observers.Length - 1];
        Array.Copy(observers, result, index);
        Array.Copy(observers, index + 1, result, index, observers.Length - index - 1);
        return result;
    }

    private sealed class FormatSubscription(LinguaFormatObservable parent, IObserver<string?> observer) : IDisposable
    {
        private int _disposed;

        public void OnNext(string value)
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

    private sealed class FormatObserver(LinguaFormatObservable parent) : IObserver<string?>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(string? value) => parent.OnFormatValue(value);
    }

    private sealed class ArgObserver(LinguaFormatObservable parent, int argIndex) : IObserver<object?>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(object? value) => parent.OnArgValue(argIndex, value);
    }

    private sealed class CultureObserver(LinguaFormatObservable parent) : IObserver<CultureInfo>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(CultureInfo value) => parent.OnCultureChanged();
    }
}
