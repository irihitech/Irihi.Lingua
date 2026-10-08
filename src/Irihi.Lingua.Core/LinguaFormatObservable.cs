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
///   pushes a new culture — but only when <c>subscribeCultureChanges</c> was
///   requested.  When the format template is a manager key observable (the
///   <c>LinguaKey</c>-based <c>Format</c> overload), this subscription is
///   redundant: <c>UpdateCulture</c> pushes every key observable after
///   switching <c>CurrentCulture</c>, so the template push alone carries the
///   culture change.  It is only needed for custom template sources (e.g.
///   <see cref="LinguaObservableString.FromLiteral"/>) that never react to
///   culture changes on their own.</item>
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
/// </remarks>
internal sealed class LinguaFormatObservable : IObservable<string?>
{
    private readonly ILinguaManager _manager;
    private readonly IObservable<string?> _format;
    private readonly bool _subscribeCultureChanges;
    private readonly object?[] _currentArgs;
    private readonly Func<IObserver<object?>, IDisposable>[] _sourceSubscribers;
    private readonly int[] _sourceArgIndices;

#if NET9_0_OR_GREATER
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    private volatile IObserver<string?>[] _observers = [];
    private bool _attached;
    private bool _attaching;
    private string? _currentFormat;
    private string? _lastEmitted;
    private IDisposable? _formatSubscription;
    private IDisposable? _cultureSubscription;
    private IDisposable[] _argSubscriptions = [];

    /// <summary>
    /// Initializes the combined observable.  Each entry of <paramref name="args"/>
    /// is either a constant value (boxed as-is) or an <see cref="IObservable{T}"/>
    /// that supplies the argument dynamically.
    /// </summary>
    /// <param name="subscribeCultureChanges">
    /// Pass <c>false</c> when the format template is a manager key observable —
    /// <c>UpdateCulture</c> pushes every key observable, so the template push
    /// alone re-triggers the recompute on culture changes.  Pass <c>true</c>
    /// (the default, and the safe choice for custom template sources) to also
    /// subscribe the manager's <see cref="ILinguaManager.CultureChanges"/> stream.
    /// </param>
    public LinguaFormatObservable(
        ILinguaManager manager,
        IObservable<string?> format,
        object?[] args,
        bool subscribeCultureChanges = true)
    {
        _manager = manager;
        _format = format;
        _subscribeCultureChanges = subscribeCultureChanges;
        _currentArgs = new object?[args.Length];

        var subscribers = new List<Func<IObserver<object?>, IDisposable>>(args.Length);
        var indices = new List<int>(args.Length);
        for (var i = 0; i < args.Length; i++)
        {
            // .NET generic covariance only covers reference conversions, so an
            // observable of a value type (e.g. IObservable<int>) does not match
            // IObservable<object?>.  The IBoxedObservable path covers Lingua's
            // own observables without reflection; other user observables can be
            // adapted beforehand with the Box() extension.
            Func<IObserver<object?>, IDisposable>? subscriber = null;
            if (args[i] is IObservable<object?> referenceSource)
                subscriber = referenceSource.Subscribe;
            else if (args[i] is IBoxedObservable boxedSource)
                subscriber = boxedSource.SubscribeBoxed;

            if (subscriber is not null)
            {
                subscribers.Add(subscriber);
                indices.Add(i);
            }
            else
            {
                _currentArgs[i] = args[i];
            }
        }

        _sourceSubscribers = subscribers.ToArray();
        _sourceArgIndices = indices.ToArray();
    }

    /// <summary>
    /// Subscribes an observer.  The observer immediately receives the result
    /// formatted from the current template, arguments and culture, then every
    /// recomputed value.
    /// </summary>
    public IDisposable Subscribe(IObserver<string?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        bool becameFirst;
        lock (_gate)
        {
            _observers = [.. _observers, observer];
            becameFirst = !_attached;
            if (becameFirst)
            {
                _attached = true;
                _attaching = true;
            }
        }

        if (becameFirst)
        {
            // Subscribing a LinguaObservable pushes its current value
            // synchronously; those pushes only prime the snapshots while
            // _attaching is set, so the initial emission below happens
            // exactly once.
            Attach();
            string initial;
            lock (_gate)
            {
                _attaching = false;
                _lastEmitted = ComputeCore();
                initial = _lastEmitted;
            }

            observer.OnNext(initial);
        }
        else
        {
            observer.OnNext(ComputeCurrent());
        }

        return new FormatSubscription(this, observer);
    }

    private void Attach()
    {
        _formatSubscription = _format.Subscribe(new FormatObserver(this));

        _argSubscriptions = new IDisposable[_sourceSubscribers.Length];
        for (var i = 0; i < _sourceSubscribers.Length; i++)
        {
            _argSubscriptions[i] = _sourceSubscribers[i](new ArgObserver(this, _sourceArgIndices[i]));
        }

        if (_subscribeCultureChanges)
        {
            _cultureSubscription = _manager.CultureChanges.Subscribe(new CultureObserver(this));
        }
    }

    private void OnFormatValue(string? value)
    {
        IObserver<string?>[] observers;
        string result;
        lock (_gate)
        {
            _currentFormat = value;
            if (_attaching || _observers.Length == 0) return;
            result = ComputeCore();
            // Safety net: a source may push without producing a new output
            // (e.g. a custom-template path where the culture stream recomputes
            // before the new template arrives). Suppress identical consecutive
            // emissions so observers only see real changes.
            if (result == _lastEmitted) return;
            _lastEmitted = result;
            observers = _observers;
        }

        foreach (var observer in observers)
            observer.OnNext(result);
    }

    private void OnArgValue(int index, object? value)
    {
        IObserver<string?>[] observers;
        string result;
        lock (_gate)
        {
            _currentArgs[index] = value;
            if (_attaching || _observers.Length == 0) return;
            result = ComputeCore();
            if (result == _lastEmitted) return;
            _lastEmitted = result;
            observers = _observers;
        }

        foreach (var observer in observers)
            observer.OnNext(result);
    }

    private void OnCultureChanged()
    {
        IObserver<string?>[] observers;
        string result;
        lock (_gate)
        {
            if (_attaching || _observers.Length == 0) return;
            result = ComputeCore();
            if (result == _lastEmitted) return;
            _lastEmitted = result;
            observers = _observers;
        }

        foreach (var observer in observers)
            observer.OnNext(result);
    }

    private string ComputeCurrent()
    {
        lock (_gate)
        {
            return ComputeCore();
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

    internal void Unsubscribe(IObserver<string?> observer)
    {
        IDisposable? formatSub;
        IDisposable? cultureSub;
        IDisposable[] argSubs;

        lock (_gate)
        {
            _observers = _observers.Where(o => !ReferenceEquals(o, observer)).ToArray();
            if (_observers.Length > 0 || !_attached)
                return;

            _attached = false;
            formatSub = _formatSubscription;
            _formatSubscription = null;
            cultureSub = _cultureSubscription;
            _cultureSubscription = null;
            argSubs = _argSubscriptions;
            _argSubscriptions = [];
        }

        formatSub?.Dispose();
        cultureSub?.Dispose();
        foreach (var subscription in argSubs)
            subscription?.Dispose();
    }

    private sealed class FormatSubscription(LinguaFormatObservable parent, IObserver<string?> observer) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                parent.Unsubscribe(observer);
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
