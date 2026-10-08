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

        var formatted = manager.Keys("Fmt").Format().Arg(3).Arg(10);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page 3 of 10", single);
    }

    [Fact]
    public void Format_ManagerOverload_WithLiteralTemplate_EmitsImmediately()
    {
        var manager = CreateManager();

        var formatted = manager
            .Format(LinguaObservableString.FromLiteral("Hello {0}"))
            .Arg("world");
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Hello world", single);
    }

    [Fact]
    public void Format_NoArguments_EmitsTemplateAsIs()
    {
        var manager = CreateManager();

        var formatted = manager.Keys("Name").Format();
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page", single);
    }

    // ── Argument kinds ───────────────────────────────────────────────────────

    [Fact]
    public void Format_ObservableArgument_PushTriggersReformat()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

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

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(99);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Page 2 of 99", received[0]);

        page.OnNext(5);
        Assert.Equal("Page 5 of 99", received[1]);
    }

    [Fact]
    public void Format_OtherResourceKeyObservable_CanBeUsedAsArgument()
    {
        var manager = CreateManager();

        var formatted = manager
            .Format(LinguaObservableString.FromLiteral("{0}: {1}"))
            .Arg(manager.GetObservable("Name")!)
            .Arg(7);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Page: 7", received[0]);
    }

    [Fact]
    public void Format_ValueTypeObservableFromOtherLibrary_CanBeUsedAsArgument()
    {
        // Value-type observables are boxed by the Arg<T> overload — no
        // adaptation needed, even for observables outside the Lingua family.
        var manager = CreateManager();
        var page = new CustomIntObservable(1);

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        page.OnNext(6);

        Assert.Equal(2, received.Count);
        Assert.Equal("Page 1 of 10", received[0]);
        Assert.Equal("Page 6 of 10", received[1]);
    }

    [Fact]
    public void Format_NullArgument_IsFormattedAsEmpty()
    {
        var manager = CreateManager();

        var formatted = manager.Keys("Fmt").Format().Arg((object?)null).Arg(10);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Page  of 10", received[0]);
    }

    // ── Culture changes ──────────────────────────────────────────────────────

    [Fact]
    public void Format_CultureChange_SwitchesTemplateText()
    {
        var manager = CreateManager()
            .Add(new CultureInfo("zh-Hans"), ("Fmt", "第{0}页 共{1}页"), ("Name", "页"));

        var formatted = manager.Keys("Fmt").Format().Arg(3).Arg(10);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

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
            .Format(LinguaObservableString.FromLiteral("Value: {0:F1}"))
            .Arg(1.5);
        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

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
                .Format(LinguaObservableString.FromLiteral("Value: {0:F1}"))
                .Arg(1.5);

            var received = new List<string?>();
            formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

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
            .Format(LinguaObservableString.FromLiteral(null))
            .Arg("arg");

        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal(string.Empty, single);
    }

    [Fact]
    public void Format_InvalidTemplate_EmitsRawTemplate()
    {
        var manager = CreateManager();
        var formatted = manager
            .Format(LinguaObservableString.FromLiteral("{0"))
            .Arg("arg");

        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("{0", single);
    }

    // ── Subscription lifetime ────────────────────────────────────────────────

    [Fact]
    public void Format_Dispose_StopsReceivingUpdates()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var received = new List<string?>();
        var subscription = formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        subscription.Dispose();
        page.OnNext(5);
        manager.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.Single(received);
    }

    [Fact]
    public void Format_Dispose_CalledTwice_DoesNotThrow()
    {
        var manager = CreateManager();
        var formatted = manager.Keys("Fmt").Format().Arg(1).Arg(2);

        var subscription = formatted.Subscribe(new DelegateObserver<string?>(_ => { }));
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

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var first = new List<string?>();
        var subscription = formatted.Subscribe(new DelegateObserver<string?>(v => first.Add(v)));
        subscription.Dispose();

        page.OnNext(8);

        var second = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => second.Add(v)));

        Assert.Single(first);
        var single = Assert.Single(second);
        Assert.Equal("Page 8 of 10", single);
    }

    [Fact]
    public void Format_MultipleObservers_AllReceiveUpdates()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        formatted.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

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

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        var subA = formatted.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        formatted.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        subA.Dispose();
        page.OnNext(2);

        Assert.Single(receivedA);
        Assert.Equal(2, receivedB.Count);
    }

    // ── Builder behaviour ────────────────────────────────────────────────────

    [Fact]
    public void FormatBuilder_FrozenAfterFirstSubscription_ThrowsOnFurtherArg()
    {
        var manager = CreateManager();

        var builder = manager.Keys("Fmt").Format().Arg(1);
        builder.Subscribe(new DelegateObserver<string?>(_ => { }));

        Assert.Throws<InvalidOperationException>(() => builder.Arg(2));
        Assert.Throws<InvalidOperationException>(() =>
            builder.Arg(new LinguaObservable<int>("page", 1)));
    }

    [Fact]
    public void FormatBuilder_MultipleSubscriptions_ShareOneCombiner()
    {
        // Both subscriptions go through the same lazily-created combiner, so
        // they observe the same stream.
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var builder = manager.Keys("Fmt").Format().Arg(page).Arg(10);
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        builder.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        builder.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        page.OnNext(2);

        Assert.Equal("Page 2 of 10", receivedA[1]);
        Assert.Equal("Page 2 of 10", receivedB[1]);
    }

    // ── Argument validation ──────────────────────────────────────────────────

    [Fact]
    public void Format_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LinguaFormatExtensions.Format(null!));
    }

    [Fact]
    public void Format_UnknownKey_ThrowsArgumentException()
    {
        var manager = CreateManager();
        Assert.Throws<ArgumentException>(() =>
            manager.Keys("NoSuchKey").Format());
    }

    [Fact]
    public void Format_ManagerOverload_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LinguaFormatExtensions.Format(null!, LinguaObservableString.FromLiteral("x")));
    }

    [Fact]
    public void Format_ManagerOverload_NullFormat_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CreateManager().Format(null!));
    }

    [Fact]
    public void Format_NullObservableArg_ThrowsArgumentNullException()
    {
        var manager = CreateManager();
        Assert.Throws<ArgumentNullException>(() =>
            manager.Keys("Fmt").Format().Arg<int>(null!));
    }

    // ── Thread safety smoke test ─────────────────────────────────────────────

    [Fact]
    public async Task Format_ConcurrentSubscribeUpdateDispose_DoesNotThrow()
    {
        var manager = CreateManager();
        var page = new LinguaObservable<int>("page", 1);

        var formatted = manager.Keys("Fmt").Format().Arg(page).Arg(10);

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            var sub = formatted.Subscribe(new DelegateObserver<string?>(_ => { }));
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
}
