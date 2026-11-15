using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaSwitchObservableTests
{
    [Fact]
    public void SourceSwitch_AndCultureChanges_FollowSelectedResource()
    {
        var manager = new FakeLinguaManager()
            .Add(CultureInfo.InvariantCulture, ("Start", "Start"), ("Stop", "Stop"))
            .Add(new CultureInfo("zh-CN"), ("Start", "开始"), ("Stop", "停止"));
        var start = manager.GetObservable("Start")!;
        var stop = manager.GetObservable("Stop")!;
        var text = new LinguaSwitchObservable(start);
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        manager.UpdateCulture(new CultureInfo("zh-CN"));
        text.Source = stop;
        manager.UpdateCulture(CultureInfo.InvariantCulture);
        text.Source = start;

        Assert.Equal(new[] { "Start", "开始", "停止", "Stop", "Start" }, received);
        Assert.Same(start, text.Source);
    }

    [Fact]
    public void Source_CanBeAFormattedObservable()
    {
        var manager = new FakeLinguaManager()
            .Add(CultureInfo.InvariantCulture, ("Count", "Items: {0}"))
            .Add(new CultureInfo("zh-CN"), ("Count", "数量：{0}"));
        var count = new LinguaObservable<int>("", 2);
        var formatted = manager.Keys("Count").CreateFormat().Arg(count).Build();
        var text = new LinguaSwitchObservable(LinguaObservableString.FromLiteral("Idle"));
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        text.Source = formatted;
        count.OnNext(3);
        manager.UpdateCulture(new CultureInfo("zh-CN"));

        Assert.Equal(new[] { "Idle", "Items: 2", "Items: 3", "数量：3" }, received);
    }

    [Fact]
    public void SourceSubscriptions_AreSharedAndReleasedWhenLastObserverLeaves()
    {
        var source = new TrackingSource("A");
        var text = new LinguaSwitchObservable(source);
        Assert.Equal(0, source.ActiveCount);
        var received = new List<string?>();
        var first = text.Subscribe(new Observer(_ => { }));
        var second = text.Subscribe(new Observer(received.Add));
        Assert.Equal(1, source.ActiveCount);
        first.Dispose();
        first.Dispose();
        Assert.Equal(1, source.ActiveCount);
        source.Push("B");
        second.Dispose();
        Assert.Equal(0, source.ActiveCount);

        source.Push("C");
        using var third = text.Subscribe(new Observer(received.Add));
        Assert.Equal(1, source.ActiveCount);
        Assert.Equal(new[] { "A", "B", "C" }, received);
    }

    [Fact]
    public void Switch_UnsubscribesOldSource_AndIgnoresItsLateCallbacks()
    {
        var old = new TrackingSource("A");
        var next = new TrackingSource("B");
        var text = new LinguaSwitchObservable(old);
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));
        var stale = old.LastObserver!;

        text.Source = next;
        stale.OnNext("stale");
        old.Push("inactive");
        next.Push("B2");

        Assert.Equal(0, old.ActiveCount);
        Assert.Equal(1, next.ActiveCount);
        Assert.Equal(new[] { "A", "B", "B2" }, received);
    }

    [Fact]
    public void Switch_WithoutObservers_AttachesOnlyLatestSourceOnSubscribe()
    {
        var first = new TrackingSource("A");
        var second = new TrackingSource("B");
        var text = new LinguaSwitchObservable(first) { Source = second };
        second.Push("B2");
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        Assert.Equal(0, first.ActiveCount);
        Assert.Equal(new[] { "B2" }, received);
    }

    [Fact]
    public void SameSourceAssignment_DoesNotResubscribeOrEmit()
    {
        var source = new TrackingSource("A");
        var text = new LinguaSwitchObservable(source);
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        text.Source = source;

        Assert.Equal(1, source.SubscribeCount);
        Assert.Equal(new[] { "A" }, received);
    }

    [Fact]
    public void DelayedSource_DoesNotReplayPreviousSourceValue_AndReplaysNull()
    {
        var text = new LinguaSwitchObservable(LinguaObservableString.FromLiteral("old"));
        var first = new List<string?>();
        var second = new List<string?>();
        using var a = text.Subscribe(new Observer(first.Add));
        var delayed = new TrackingSource(null, emitOnSubscribe: false);
        text.Source = delayed;
        using var b = text.Subscribe(new Observer(second.Add));
        Assert.Empty(second);

        delayed.Push(null);
        var third = new List<string?>();
        using var c = text.Subscribe(new Observer(third.Add));

        Assert.Equal(new string?[] { "old", null }, first);
        Assert.Equal(new string?[] { null }, second);
        Assert.Equal(new string?[] { null }, third);
    }

    [Fact]
    public void ReentrantSwitch_NotifiesAllObserversInOrder()
    {
        var source = LinguaObservableString.FromLiteral("A");
        var text = new LinguaSwitchObservable(source);
        var first = new List<string?>();
        var second = new List<string?>();
        using var a = text.Subscribe(new Observer(value =>
        {
            first.Add(value);
            if (value == "A2")
                text.Source = LinguaObservableString.FromLiteral("B");
        }));
        using var b = text.Subscribe(new Observer(second.Add));

        source.OnNext("A2");

        Assert.Equal(new[] { "A", "A2", "B" }, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ReentrantSubscribe_DisposedBeforeQueuedDelivery_DoesNotReceiveValues()
    {
        var source = LinguaObservableString.FromLiteral("A");
        var text = new LinguaSwitchObservable(source);
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(value =>
        {
            if (value == "B")
                text.Subscribe(new Observer(received.Add)).Dispose();
        }));

        source.OnNext("B");
        source.OnNext("C");

        Assert.Empty(received);
    }

    [Fact]
    public void SameObserverSubscribedTwice_HasIndependentSubscriptions()
    {
        var source = LinguaObservableString.FromLiteral("A");
        var text = new LinguaSwitchObservable(source);
        var received = new List<string?>();
        var observer = new Observer(received.Add);
        var first = text.Subscribe(observer);
        using var second = text.Subscribe(observer);

        first.Dispose();
        source.OnNext("B");

        Assert.Equal(new[] { "A", "A", "B" }, received);
    }

    [Fact]
    public void ThrowingInitialObserver_ReleasesSource_AndAllowsRetry()
    {
        var source = new TrackingSource("A");
        var text = new LinguaSwitchObservable(source);
        Assert.Throws<InvalidOperationException>(() =>
            text.Subscribe(new Observer(_ => throw new InvalidOperationException())));
        Assert.Equal(0, source.ActiveCount);

        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));
        Assert.Equal(new[] { "A" }, received);
    }

    [Fact]
    public void ThrowingSourceSubscription_CanBeRetried()
    {
        var source = new TrackingSource("B") { ThrowOnSubscribe = true };
        var text = new LinguaSwitchObservable(LinguaObservableString.FromLiteral("A"));
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        Assert.Throws<InvalidOperationException>(() => text.Source = source);
        source.LastObserver!.OnNext("stale");
        source.ThrowOnSubscribe = false;
        text.Source = source;

        Assert.Equal(new[] { "A", "B" }, received);
    }

    [Fact]
    public void SwitchDuringSourceSubscribe_DisposesTheLateReturnedSubscription()
    {
        var first = new TrackingSource("A");
        var second = new TrackingSource("B");
        var text = new LinguaSwitchObservable(first);
        first.DuringSubscribe = () => text.Source = second;
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        Assert.Equal(0, first.ActiveCount);
        Assert.Equal(1, second.ActiveCount);
        Assert.Equal(new[] { "B" }, received);
    }

    [Fact]
    public void SwitchDuringSourceDisposal_DoesNotAttachTheNewSelectionTwice()
    {
        var first = new TrackingSource("A");
        var second = new TrackingSource("B");
        var third = new TrackingSource("C");
        var text = new LinguaSwitchObservable(first);
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));
        first.DuringDispose = () => text.Source = third;

        text.Source = second;

        Assert.Same(third, text.Source);
        Assert.Equal(0, first.ActiveCount);
        Assert.Equal(0, second.ActiveCount);
        Assert.Equal(1, third.ActiveCount);
        Assert.Equal(1, third.SubscribeCount);
        Assert.Equal(new[] { "A", "C" }, received);
    }

    [Fact]
    public void SourceCompletionAndError_DoNotPreventLaterSwitches()
    {
        var source = new TrackingSource("A");
        var text = new LinguaSwitchObservable(source);
        var received = new List<string?>();
        using var subscription = text.Subscribe(new Observer(received.Add));

        source.LastObserver!.OnCompleted();
        text.Source = new TrackingSource("B");
        ((TrackingSource)text.Source).LastObserver!.OnError(new InvalidOperationException());
        text.Source = LinguaObservableString.FromLiteral("C");

        Assert.Equal(new[] { "A", "B", "C" }, received);
    }

    [Fact]
    public void FailedFirstSubscribe_RemovesObserver_AndCanBeRetried()
    {
        var source = new TrackingSource("A") { ThrowOnSubscribe = true };
        var text = new LinguaSwitchObservable(source);
        var failed = new List<string?>();
        Assert.Throws<InvalidOperationException>(() => text.Subscribe(new Observer(failed.Add)));

        source.ThrowOnSubscribe = false;
        var received = new List<string?>();
        using (text.Subscribe(new Observer(received.Add)))
            Assert.Equal(new[] { "A" }, received);

        Assert.Empty(failed);
        Assert.Equal(0, source.ActiveCount);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new LinguaSwitchObservable(null!));
        var source = LinguaObservableString.FromLiteral(null);
        var text = new LinguaSwitchObservable(source);
        Assert.Throws<ArgumentNullException>(() => text.Source = null!);
        Assert.Throws<ArgumentNullException>(() => text.Subscribe(null!));
        Assert.Same(source, text.Source);
    }

    private sealed class Observer(Action<string?> onNext) : IObserver<string?>
    {
        public void OnNext(string? value) => onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class TrackingSource(string? value, bool emitOnSubscribe = true) : IObservable<string?>
    {
        private readonly List<IObserver<string?>> _observers = [];
        public int ActiveCount => _observers.Count;
        public int SubscribeCount { get; private set; }
        public IObserver<string?>? LastObserver { get; private set; }
        public bool ThrowOnSubscribe { get; set; }
        public Action? DuringSubscribe { get; set; }
        public Action? DuringDispose { get; set; }

        public IDisposable Subscribe(IObserver<string?> observer)
        {
            SubscribeCount++;
            LastObserver = observer;
            if (ThrowOnSubscribe)
                throw new InvalidOperationException();
            _observers.Add(observer);
            if (emitOnSubscribe)
                observer.OnNext(value);
            DuringSubscribe?.Invoke();
            return new Cleanup(() =>
            {
                _observers.Remove(observer);
                DuringDispose?.Invoke();
            });
        }

        public void Push(string? next)
        {
            value = next;
            foreach (var observer in _observers.ToArray())
                observer.OnNext(next);
        }
    }

    private sealed class Cleanup(Action cleanup) : IDisposable
    {
        public void Dispose() => cleanup();
    }
}
