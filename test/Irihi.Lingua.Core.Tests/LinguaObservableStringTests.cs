using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaObservableStringTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LinguaObservableString(null!, "value"));
    }

    [Fact]
    public void Constructor_SetsKey()
    {
        var obs = new LinguaObservableString("MyKey", "initial");
        Assert.Equal("MyKey", obs.Key);
    }

    [Fact]
    public void Constructor_SetsInitialCurrentValue()
    {
        var obs = new LinguaObservableString("key", "hello");
        Assert.Equal("hello", obs.CurrentValue);
    }

    [Fact]
    public void Constructor_NullInitialValue_CurrentValueIsNull()
    {
        var obs = new LinguaObservableString("key", null);
        Assert.Null(obs.CurrentValue);
    }

    // ── Subscribe ────────────────────────────────────────────────────────────

    [Fact]
    public void Subscribe_NullObserver_ThrowsArgumentNullException()
    {
        var obs = new LinguaObservableString("key", "value");
        Assert.Throws<ArgumentNullException>(() =>
            obs.Subscribe(null!));
    }

    [Fact]
    public void Subscribe_ImmediatelyEmitsCurrentValue()
    {
        var obs = new LinguaObservableString("key", "initial");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("initial", single);
    }

    [Fact]
    public void Subscribe_ImmediatelyEmitsNullCurrentValue()
    {
        var obs = new LinguaObservableString("key", null);
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Single(received);
        Assert.Null(received[0]);
    }

    [Fact]
    public void Subscribe_MultipleObservers_AllReceiveCurrentValue()
    {
        var obs = new LinguaObservableString("key", "val");
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
        var obs = new LinguaObservableString("key", "initial");
        obs.OnNext("updated");
        Assert.Equal("updated", obs.CurrentValue);
    }

    [Fact]
    public void OnNext_NotifiesSubscriber()
    {
        var obs = new LinguaObservableString("key", "initial");
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
        var obs = new LinguaObservableString("key", "initial");
        obs.OnNext(null);
        Assert.Null(obs.CurrentValue);
    }

    [Fact]
    public void OnNext_NotifiesMultipleSubscribers()
    {
        var obs = new LinguaObservableString("key", "a");
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        obs.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        obs.OnNext("b");

        // each observer got the initial value + the new one
        Assert.Equal(2, receivedA.Count);
        Assert.Equal("b", receivedA[1]);
        Assert.Equal(2, receivedB.Count);
        Assert.Equal("b", receivedB[1]);
    }

    [Fact]
    public void OnNext_NoSubscribers_DoesNotThrow()
    {
        var obs = new LinguaObservableString("key", "initial");
        obs.OnNext("updated");
        Assert.Equal("updated", obs.CurrentValue);
    }

    // ── Dispose / Unsubscribe ────────────────────────────────────────────────

    [Fact]
    public void Dispose_StopsReceivingUpdates()
    {
        var obs = new LinguaObservableString("key", "initial");
        var received = new List<string?>();
        var subscription = obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        subscription.Dispose();
        obs.OnNext("after-dispose");

        Assert.Single(received); // only initial value
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var obs = new LinguaObservableString("key", "initial");
        var subscription = obs.Subscribe(new DelegateObserver<string?>(_ => { }));

        subscription.Dispose();
        subscription.Dispose(); // second dispose must be safe
    }

    [Fact]
    public void Dispose_OtherSubscribersStillReceiveUpdates()
    {
        var obs = new LinguaObservableString("key", "initial");
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();

        var subA = obs.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        obs.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        subA.Dispose();
        obs.OnNext("next");

        Assert.Single(receivedA); // A only got initial
        Assert.Equal(2, receivedB.Count); // B got initial + next
    }

    // ── Thread safety ────────────────────────────────────────────────────────

    [Fact]
    public async Task OnNext_ConcurrentSubscribersAndUpdates_DoesNotThrow()
    {
        var obs = new LinguaObservableString("key", "0");

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            var sub = obs.Subscribe(new DelegateObserver<string?>(_ => { }));
            obs.OnNext(i.ToString());
            sub.Dispose();
        }));

        await Task.WhenAll(tasks);
        // no exception = pass
    }

    // ── FromLiteral ───────────────────────────────────────────────────────────

    [Fact]
    public void FromLiteral_NonEmptyString_EmitsImmediatelyOnSubscribe()
    {
        var obs = LinguaObservableString.FromLiteral("Hello");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Hello", single);
    }

    [Fact]
    public void FromLiteral_NullValue_EmitsNullOnSubscribe()
    {
        var obs = LinguaObservableString.FromLiteral(null);
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Single(received);
        Assert.Null(received[0]);
    }

    [Fact]
    public void FromLiteral_EmptyString_EmitsEmptyString()
    {
        var obs = LinguaObservableString.FromLiteral(string.Empty);
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal(string.Empty, single);
    }

    [Fact]
    public void FromLiteral_KeyIsEmptyString()
    {
        var obs = LinguaObservableString.FromLiteral("anything");
        Assert.Equal(string.Empty, obs.Key);
    }

    [Fact]
    public void FromLiteral_CurrentValueReturnsLiteral()
    {
        var obs = LinguaObservableString.FromLiteral("literal");
        Assert.Equal("literal", obs.CurrentValue);
    }

    [Fact]
    public void FromLiteral_MultipleSubscribers_AllReceiveSameValue()
    {
        var obs = LinguaObservableString.FromLiteral("shared");
        var countA = 0;
        var countB = 0;
        obs.Subscribe(new DelegateObserver<string?>(_ => countA++));
        obs.Subscribe(new DelegateObserver<string?>(_ => countB++));

        Assert.Equal(1, countA);
        Assert.Equal(1, countB);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}