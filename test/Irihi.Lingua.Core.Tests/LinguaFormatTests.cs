using System.Runtime.CompilerServices;
using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaFormatTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static FakeLinguaManager CreateManager() => new FakeLinguaManager()
        .Add(Invariant, ("Fmt", "Page {0} of {1}"), ("Name", "Page"));

    // ── Initial emission ─────────────────────────────────────────────────────

    [Fact]
    public void Format_EmitsFormattedValueImmediatelyOnSubscribe()
    {
        var manager = CreateManager();

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(3).Arg(10);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page 3 of 10", single);
    }

    [Fact]
    public void CreateFormat_ConstantTemplateString_EmitsImmediately()
    {
        var manager = CreateManager();

        var formatted = manager
            .CreateFormat("Hello {0}")
            .Arg("world");
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Hello world", single);
    }

    [Fact]
    public void Format_NoArguments_EmitsTemplateAsIs()
    {
        var manager = CreateManager();

        var formatted = manager.Keys("Name").CreateFormat();
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page", single);
    }

    // ── Argument kinds ───────────────────────────────────────────────────────

    [Fact]
    public void Format_ObservableArgument_PushTriggersReformat()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        page.OnNext(4);

        Assert.Equal(2, received.Count);
        Assert.Equal("Page 1 of 10", received[0]);
        Assert.Equal("Page 4 of 10", received[1]);
    }

    [Fact]
    public void Format_MixedConstantAndObservableArguments_ArePlacedInOrder()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 2);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(99);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Page 2 of 99", received[0]);

        page.OnNext(5);
        Assert.Equal("Page 5 of 99", received[1]);
    }

    [Fact]
    public void Format_OtherResourceKeyObservable_CanBeUsedAsArgument()
    {
        var manager = CreateManager();

        var formatted = manager
            .CreateFormat("{0}: {1}")
            .Arg(manager.GetObservable("Name")!)
            .Arg(7);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Page: 7", received[0]);
    }

    [Fact]
    public void Format_ValueTypeObservableFromOtherLibrary_CanBeUsedAsArgument()
    {
        // Value-type observables are boxed by the Arg<T> overload — no
        // adaptation needed, even for observables outside the Lingua family.
        var manager = CreateManager();
        var page = new CustomIntObservable(1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        page.OnNext(6);

        Assert.Equal(2, received.Count);
        Assert.Equal("Page 1 of 10", received[0]);
        Assert.Equal("Page 6 of 10", received[1]);
    }

    [Fact]
    public void Format_NullArgument_IsFormattedAsEmpty()
    {
        var manager = CreateManager();

        var formatted = manager.Keys("Fmt").CreateFormat().Arg((object?)null).Arg(10);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Page  of 10", received[0]);
    }

    // ── Culture changes ──────────────────────────────────────────────────────

    [Fact]
    public void Format_CultureChange_SwitchesTemplateText()
    {
        var manager = CreateManager()
            .Add(new CultureInfo("zh-Hans"), ("Fmt", "第{0}页 共{1}页"), ("Name", "页"));

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(3).Arg(10);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        manager.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.Equal(2, received.Count);
        Assert.Equal("Page 3 of 10", received[0]);
        Assert.Equal("第3页 共10页", received[1]);
    }

    [Fact]
    public void Format_CultureChange_ReformatsNumbersEvenWhenTemplateUnchanged()
    {
        // The template only exists for the invariant culture; switching to
        // de-DE falls back to the same template text, but the number format
        // must still follow the manager's active culture.
        var manager = CreateManager()
            .Add(new CultureInfo("de-DE"), ("Name", "Seite"));

        var formatted = manager
            .CreateFormat(LinguaObservableString.FromLiteral("Value: {0:F1}"))
            .Arg(1.5);
        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        manager.UpdateCulture(new CultureInfo("de-DE"));

        Assert.Equal(2, received.Count);
        Assert.Equal("Value: 1.5", received[0]);
        Assert.Equal("Value: 1,5", received[1]);
    }

    [Fact]
    public void Format_UsesManagerCultureNotThreadCulture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var manager = CreateManager();
            var formatted = manager
                .CreateFormat("Value: {0:F1}")
                .Arg(1.5);

            var received = new List<string?>();
            formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

            Assert.Equal("Value: 1.5", received[0]);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ── Defensive behaviour ──────────────────────────────────────────────────

    [Fact]
    public void Format_NullTemplateValue_EmitsEmptyString()
    {
        var manager = CreateManager();
        var formatted = manager
            .CreateFormat(LinguaObservableString.FromLiteral(null))
            .Arg("arg");

        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal(string.Empty, single);
    }

    [Fact]
    public void Format_InvalidTemplate_EmitsRawTemplate()
    {
        var manager = CreateManager();
        var formatted = manager
            .CreateFormat("{0")
            .Arg("arg");

        var received = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("{0", single);
    }

    // ── Subscription lifetime ────────────────────────────────────────────────

    [Fact]
    public void Format_Dispose_StopsReceivingUpdates()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);
        var received = new List<string?>();
        var subscription = formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        subscription.Dispose();
        page.OnNext(5);
        manager.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.Single(received);
    }

    [Fact]
    public void Format_Dispose_CalledTwice_DoesNotThrow()
    {
        var manager = CreateManager();
        var formatted = manager.Keys("Fmt").CreateFormat().Arg(1).Arg(2);

        var subscription = formatted.Build().Subscribe(new DelegateObserver<string?>(_ => { }));
        subscription.Dispose();
        subscription.Dispose();
    }

    [Fact]
    public void Format_ResubscribeAfterAllUnsubscribed_UsesLatestValues()
    {
        // After all subscribers dispose, the combined observable detaches from
        // its sources; a later subscription re-attaches and must reflect the
        // values the sources pushed in the meantime.
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);
        var first = new List<string?>();
        var subscription = formatted.Build().Subscribe(new DelegateObserver<string?>(v => first.Add(v)));
        subscription.Dispose();

        page.OnNext(8);

        var second = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => second.Add(v)));

        Assert.Single(first);
        var single = Assert.Single(second);
        Assert.Equal("Page 8 of 10", single);
    }

    [Fact]
    public void Format_MultipleObservers_AllReceiveUpdates()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        page.OnNext(2);

        Assert.Equal(2, receivedA.Count);
        Assert.Equal(2, receivedB.Count);
        Assert.Equal("Page 2 of 10", receivedA[1]);
        Assert.Equal("Page 2 of 10", receivedB[1]);
    }

    [Fact]
    public void Format_DisposeOneObserver_OtherStillReceivesUpdates()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        var subA = formatted.Build().Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        subA.Dispose();
        page.OnNext(2);

        Assert.Single(receivedA);
        Assert.Equal(2, receivedB.Count);
    }

    // ── Builder behaviour ────────────────────────────────────────────────────

    [Fact]
    public void FormatBuilder_FrozenAfterBuild_ThrowsOnFurtherArg()
    {
        var manager = CreateManager();

        var builder = manager.Keys("Fmt").CreateFormat().Arg(1);
        builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Arg(2));
        Assert.Throws<InvalidOperationException>(() =>
            builder.Arg(new LinguaObservable<int>("page", 1)));
    }

    [Fact]
    public void FormatBuilder_BuildOnce_StreamSharedByAllSubscribers()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10).Build();
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        formatted.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        page.OnNext(2);

        Assert.Equal("Page 2 of 10", receivedA[1]);
        Assert.Equal("Page 2 of 10", receivedB[1]);
    }

    // ── Computed arguments and Refresh ───────────────────────────────────────

    [Fact]
    public void CreateFormat_ComputedArg_EmitsInitialValueOnSubscribe()
    {
        var manager = CreateManager();
        var plain = new PlainModel { Total = 7 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(3)
            .Arg(() => plain.Total)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page 3 of 7", single);
    }

    [Fact]
    public void CreateFormat_ComputedArg_WithoutRefresh_DoesNotEmit()
    {
        var manager = CreateManager();
        var plain = new PlainModel { Total = 7 };

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(3)
            .Arg(() => plain.Total)
            .Build();
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        plain.Total = 9; // no notifications exist for a computed arg
        Assert.Single(received);

        formatted.Refresh();
        Assert.Equal(2, received.Count);
        Assert.Equal("Page 3 of 9", received[1]);
    }

    [Fact]
    public void Refresh_RereadsLivePropertyGetters()
    {
        var manager = CreateManager();
        var model = new SilentModel { Value = 1 }; // INPC, but never raises

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(3)
            .Arg(model, nameof(SilentModel.Value), s => s.Value)
            .Build();
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Value = 8; // silent change, no event
        Assert.Single(received);

        formatted.Refresh();
        Assert.Equal("Page 3 of 8", received[1]);
    }

    [Fact]
    public void Refresh_NotifiesEvenWhenResultUnchanged()
    {
        var manager = CreateManager();

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(3)
            .Arg(10)
            .Build();
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        formatted.Refresh();
        formatted.Refresh();

        Assert.Equal(3, received.Count); // explicit refresh bypasses dedup
        Assert.All(received, v => Assert.Equal("Page 3 of 10", v));
    }

    [Fact]
    public void Refresh_WithNoSubscribers_NextSubscribeSeesFreshValues()
    {
        var manager = CreateManager();
        var plain = new PlainModel { Total = 7 };

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(3)
            .Arg(() => plain.Total)
            .Build();

        plain.Total = 12;
        formatted.Refresh(); // nobody listening — must not throw

        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page 3 of 12", single);
    }

    [Fact]
    public void CreateFormat_ComputedArg_NullGetter_ThrowsArgumentNullException()
    {
        var manager = CreateManager();
        Assert.Throws<ArgumentNullException>(() =>
            manager.Keys("Fmt").CreateFormat().Arg((Func<int>)null!));
    }

    // ── Memory-leak guards ───────────────────────────────────────────────────
    //
    // The build/subscribe/dispose sequence lives in a [NoInlining] helper so
    // no local (and no xunit state-machine field promotion) keeps the combiner
    // alive; only the WeakReference crosses back to the test method.

    [Fact]
    public void Format_AfterFullUnsubscribe_CombinerIsCollectable()
    {
        // Covers the format-template subscription and observable-arg
        // subscriptions: once the last subscriber disposes, no source holds
        // the combiner and the whole graph must be collectable.
        var weak = BuildSubscribeAndDispose(CreateManager());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive);
    }

    [Fact]
    public void Format_AfterFullUnsubscribe_CultureSubscriptionAndPropertyEventAreUnwired()
    {
        // Covers the CultureChanges subscription (custom-template entry) and
        // the PropertyChanged wiring: both must be released when the last
        // subscriber disposes, or the manager/source would root the combiner.
        var weak = BuildSubscribeAndDisposeWithProperty(CreateManager());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BuildSubscribeAndDispose(FakeLinguaManager manager)
    {
        var page = new LinguaObservable<int>("page", 1);
        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10).Build();
        var weak = new WeakReference(formatted);

        formatted.Subscribe(new DelegateObserver<string?>(_ => { })).Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BuildSubscribeAndDisposeWithProperty(FakeLinguaManager manager)
    {
        var model = new SilentModel { Value = 1 };
        var formatted = manager.CreateFormat("Page {0} of {1}")
            .Arg(model, nameof(SilentModel.Value), s => s.Value)
            .Arg(10)
            .Build();
        var weak = new WeakReference(formatted);

        formatted.Subscribe(new DelegateObserver<string?>(_ => { })).Dispose();

        return weak;
    }

    [Fact]
    public void Format_SourceSubscriptionThrowsDuringAttach_StateIsRolledBackAndRetryWorks()
    {
        var manager = CreateManager();
        var flaky = new FlakyObservable(failures: 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(flaky).Arg(10).Build();

        Assert.Throws<InvalidOperationException>(() =>
            formatted.Subscribe(new DelegateObserver<string?>(_ => { })));

        // the failed attachment was rolled back: subscribing again attaches
        // from scratch and works
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page 5 of 10", single);
    }

    [Fact]
    public void Format_SameObserverSubscribedTwice_DisposingOneKeepsTheOther()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10).Build();
        var received = new List<string?>();
        var observer = new DelegateObserver<string?>(v => received.Add(v));
        var subscription1 = formatted.Subscribe(observer);
        var subscription2 = formatted.Subscribe(observer);

        subscription1.Dispose();
        page.OnNext(2); // one live registration remains

        Assert.Equal(new[] { "Page 1 of 10", "Page 1 of 10", "Page 2 of 10" }, received);

        subscription2.Dispose();
    }

    // ── Argument validation ──────────────────────────────────────────────────

    [Fact]
    public void CreateFormat_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LinguaFormatExtensions.CreateFormat(null!));
    }

    [Fact]
    public void CreateFormat_UnknownKey_ThrowsArgumentException()
    {
        var manager = CreateManager();
        Assert.Throws<ArgumentException>(() =>
            manager.Keys("NoSuchKey").CreateFormat());
    }

    [Fact]
    public void CreateFormat_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LinguaFormatExtensions.CreateFormat(null!, LinguaObservableString.FromLiteral("x")));
        Assert.Throws<ArgumentNullException>(() =>
            LinguaFormatExtensions.CreateFormat(null!, "x"));
    }

    [Fact]
    public void CreateFormat_NullTemplateObservable_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CreateManager().CreateFormat((IObservable<string?>)null!));
    }

    [Fact]
    public void CreateFormat_NullTemplateString_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CreateManager().CreateFormat((string)null!));
    }

    [Fact]
    public void Format_NullObservableArg_ThrowsArgumentNullException()
    {
        var manager = CreateManager();
        Assert.Throws<ArgumentNullException>(() =>
            manager.Keys("Fmt").CreateFormat().Arg((IObservable<int>)null!));
    }

    // ── Thread safety smoke test ─────────────────────────────────────────────

    [Fact]
    public async Task Format_ConcurrentFirstSubscriptions_AllReceiveCompleteInitialValue()
    {
        // Regression guard: a subscriber arriving while the first one is still
        // attaching must not observe half-initialized snapshots.
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);
        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10).Build();

        var first = new System.Collections.Concurrent.ConcurrentQueue<string?>();
        var barrier = new System.Threading.Barrier(8);
        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait();
            formatted.Subscribe(new DelegateObserver<string?>(v => first.Enqueue(v)));
        }));

        await Task.WhenAll(tasks);

        // every subscriber's initial emission must be the complete value
        Assert.Equal(8, first.Count);
        Assert.All(first, v => Assert.Equal("Page 1 of 10", v));
    }

    [Fact]
    public async Task Format_ConcurrentPushesDuringSubscribe_AreNeverLost()
    {
        // Regression guard for the attaching window: a cross-thread push that
        // starts while the first subscriber is attaching must still produce an
        // emission once attachment completes — its emit decision is made under
        // the gate, after the initial emission.
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 0);
        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10).Build();

        var received = new System.Collections.Concurrent.ConcurrentQueue<string?>();
        var pusher = Task.Run(() =>
        {
            for (var i = 1; i <= 1000; i++)
                page.OnNext(i);
        });

        // subscribe while pushes are in flight, and stay subscribed
        await Task.Yield();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Enqueue(v)));
        await pusher;

        Assert.Equal("Page 1000 of 10", received.Last());
    }

    [Fact]
    public async Task Format_ConcurrentSubscribeUpdateDispose_DoesNotThrow()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").CreateFormat().Arg(page).Arg(10);

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            var sub = formatted.Build().Subscribe(new DelegateObserver<string?>(_ => { }));
            page.OnNext(i);
            manager.UpdateCulture(i % 2 == 0 ? Invariant : new CultureInfo("zh-Hans"));
            sub.Dispose();
        }));

        await Task.WhenAll(tasks);
        // no exception = pass
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }

    /// <summary>
    /// A value-type observable that is not part of the Lingua type family,
    /// mimicking observables from other libraries.
    /// </summary>
    private sealed class CustomIntObservable(int initial) : IObservable<int>
    {
        private readonly List<IObserver<int>> _observers = [];
        private int _value = initial;

        public void OnNext(int value)
        {
            _value = value;
            foreach (var observer in _observers.ToArray())
                observer.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<int> observer)
        {
            _observers.Add(observer);
            observer.OnNext(_value);
            return new Subscription(() => _observers.Remove(observer));
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    dispose();
            }
        }
    }

    /// <summary>A plain holder without INotifyPropertyChanged.</summary>
    private sealed class PlainModel
    {
        public int Total { get; set; }
    }

    /// <summary>An INPC source that never raises PropertyChanged.</summary>
    private sealed class SilentModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public int Value { get; set; }
    }

    /// <summary>An observable whose first Subscribe calls throw.</summary>
    private sealed class FlakyObservable(int failures) : IObservable<int>
    {
        private int _failuresLeft = failures;

        public IDisposable Subscribe(IObserver<int> observer)
        {
            if (_failuresLeft-- > 0)
                throw new InvalidOperationException("boom");

            observer.OnNext(5);
            return new NoopDisposable();
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
