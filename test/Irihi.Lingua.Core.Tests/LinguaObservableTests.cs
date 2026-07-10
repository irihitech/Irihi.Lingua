using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

/// <summary>
/// Unit tests for <see cref="LinguaObservable{T}"/> behavior-subject semantics.
/// Validates Subscribe, OnNext, Dispose, thread safety, and integration with
/// <see cref="LinguaObservableString"/> as the derived type.
/// </summary>
public class LinguaObservableTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LinguaObservable<string?>(null!, "value"));
    }

    [Fact]
    public void Constructor_SetsKey()
    {
        var obs = new LinguaObservable<string?>("MyKey", "initial");
        Assert.Equal("MyKey", obs.Key);
    }

    [Fact]
    public void Constructor_SetsInitialCurrentValue()
    {
        var obs = new LinguaObservable<int>("key", 42);
        Assert.Equal(42, obs.CurrentValue);
    }

    [Fact]
    public void Constructor_ReferenceType_NullInitialValue()
    {
        var obs = new LinguaObservable<string?>("key", null);
        Assert.Null(obs.CurrentValue);
    }

    [Fact]
    public void Constructor_ValueType_DefaultInitialValue()
    {
        var obs = new LinguaObservable<int>("key", 0);
        Assert.Equal(0, obs.CurrentValue);
    }

    // ── Subscribe ────────────────────────────────────────────────────────────

    [Fact]
    public void Subscribe_NullObserver_ThrowsArgumentNullException()
    {
        var obs = new LinguaObservable<string?>("key", "value");
        Assert.Throws<ArgumentNullException>(() =>
            obs.Subscribe(null!));
    }

    [Fact]
    public void Subscribe_ImmediatelyEmitsCurrentValue_ReferenceType()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("initial", single);
    }

    [Fact]
    public void Subscribe_ImmediatelyEmitsCurrentValue_ValueType()
    {
        var obs = new LinguaObservable<int>("key", 7);
        var received = new List<int>();
        obs.Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal(7, single);
    }

    [Fact]
    public void Subscribe_ImmediatelyEmitsNullCurrentValue()
    {
        var obs = new LinguaObservable<string?>("key", null);
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Single(received);
        Assert.Null(received[0]);
    }

    [Fact]
    public void Subscribe_MultipleObservers_AllReceiveCurrentValue()
    {
        var obs = new LinguaObservable<string?>("key", "val");
        var countA = 0;
        var countB = 0;
        obs.Subscribe(new DelegateObserver<string?>(_ => countA++));
        obs.Subscribe(new DelegateObserver<string?>(_ => countB++));

        Assert.Equal(1, countA);
        Assert.Equal(1, countB);
    }

    // ── OnNext ───────────────────────────────────────────────────────────────

    [Fact]
    public void OnNext_UpdatesCurrentValue()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        obs.OnNext("updated");
        Assert.Equal("updated", obs.CurrentValue);
    }

    [Fact]
    public void OnNext_NotifiesSubscriber()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        obs.OnNext("next");

        Assert.Equal(2, received.Count); // initial + next
        Assert.Equal("initial", received[0]);
        Assert.Equal("next", received[1]);
    }

    [Fact]
    public void OnNext_NullValue_UpdatesCurrentValueToNull()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        obs.OnNext(null);
        Assert.Null(obs.CurrentValue);
    }

    [Fact]
    public void OnNext_NotifiesMultipleSubscribers()
    {
        var obs = new LinguaObservable<int>("key", 1);
        var receivedA = new List<int>();
        var receivedB = new List<int>();
        obs.Subscribe(new DelegateObserver<int>(v => receivedA.Add(v)));
        obs.Subscribe(new DelegateObserver<int>(v => receivedB.Add(v)));

        obs.OnNext(2);

        Assert.Equal(2, receivedA.Count);
        Assert.Equal(2, receivedA[1]);
        Assert.Equal(2, receivedB.Count);
        Assert.Equal(2, receivedB[1]);
    }

    [Fact]
    public void OnNext_NoSubscribers_DoesNotThrow()
    {
        var obs = new LinguaObservable<int>("key", 0);
        obs.OnNext(42);
        Assert.Equal(42, obs.CurrentValue);
    }

    // ── Dispose / Unsubscribe ────────────────────────────────────────────────

    [Fact]
    public void Dispose_StopsReceivingUpdates()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        var received = new List<string?>();
        var subscription = obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        subscription.Dispose();
        obs.OnNext("after-dispose");

        Assert.Single(received); // only initial value
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        var subscription = obs.Subscribe(new DelegateObserver<string?>(_ => { }));

        subscription.Dispose();
        subscription.Dispose();
    }

    [Fact]
    public void Dispose_OtherSubscribersStillReceiveUpdates()
    {
        var obs = new LinguaObservable<string?>("key", "initial");
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();

        var subA = obs.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        obs.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        subA.Dispose();
        obs.OnNext("next");

        Assert.Single(receivedA); // A only got initial
        Assert.Equal(2, receivedB.Count); // B got initial + next
    }

    // ── CultureInfo-specific ─────────────────────────────────────────────────

    [Fact]
    public void CultureInfoObservable_InitialValueIsStored()
    {
        var zhHans = new CultureInfo("zh-Hans");
        var obs = new LinguaObservable<CultureInfo>("culture", zhHans);
        Assert.Same(zhHans, obs.CurrentValue);
    }

    [Fact]
    public void CultureInfoObservable_SubscribeEmitsInitialCulture()
    {
        var invariant = CultureInfo.InvariantCulture;
        var obs = new LinguaObservable<CultureInfo>("culture", invariant);
        var received = new List<CultureInfo>();
        obs.Subscribe(new DelegateObserver<CultureInfo>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Same(invariant, single);
    }

    [Fact]
    public void CultureInfoObservable_OnNextNotifies()
    {
        var obs = new LinguaObservable<CultureInfo>("culture", CultureInfo.InvariantCulture);
        var received = new List<CultureInfo>();
        obs.Subscribe(new DelegateObserver<CultureInfo>(v => received.Add(v)));

        var zhHans = new CultureInfo("zh-Hans");
        obs.OnNext(zhHans);

        Assert.Equal(2, received.Count);
        Assert.Same(CultureInfo.InvariantCulture, received[0]);
        Assert.Same(zhHans, received[1]);
    }

    // ── Thread safety ────────────────────────────────────────────────────────

    [Fact]
    public async Task OnNext_ConcurrentSubscribersAndUpdates_DoesNotThrow()
    {
        var obs = new LinguaObservable<int>("key", 0);

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            var sub = obs.Subscribe(new DelegateObserver<int>(_ => { }));
            obs.OnNext(i);
            sub.Dispose();
        }));

        await Task.WhenAll(tasks);
        // no exception = pass
    }

    // ── LinguaObservableString backward compat ───────────────────────────────

    [Fact]
    public void LinguaObservableString_IsLinguaObservableOfString()
    {
        var obs = new LinguaObservableString("key", "value");
        Assert.IsType<LinguaObservableString>(obs);
        Assert.IsAssignableFrom<LinguaObservable<string?>>(obs);
    }

    [Fact]
    public void LinguaObservableString_KeyAndCurrentValueWork()
    {
        var obs = new LinguaObservableString("MyKey", "hello");
        Assert.Equal("MyKey", obs.Key);
        Assert.Equal("hello", obs.CurrentValue);
    }

    [Fact]
    public void LinguaObservableString_SubscribeWorks()
    {
        var obs = new LinguaObservableString("key", "initial");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Single(received);
        Assert.Equal("initial", received[0]);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
