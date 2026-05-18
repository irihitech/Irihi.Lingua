using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lolita.Tests;

[TestClass]
public class LolitaObservableStringTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LolitaObservableString(null!, "value"));
    }

    [TestMethod]
    public void Constructor_SetsKey()
    {
        var obs = new LolitaObservableString("MyKey", "initial");
        Assert.AreEqual("MyKey", obs.Key);
    }

    [TestMethod]
    public void Constructor_SetsInitialCurrentValue()
    {
        var obs = new LolitaObservableString("key", "hello");
        Assert.AreEqual("hello", obs.CurrentValue);
    }

    [TestMethod]
    public void Constructor_NullInitialValue_CurrentValueIsNull()
    {
        var obs = new LolitaObservableString("key", null);
        Assert.IsNull(obs.CurrentValue);
    }

    // ── Subscribe ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Subscribe_NullObserver_ThrowsArgumentNullException()
    {
        var obs = new LolitaObservableString("key", "value");
        Assert.ThrowsException<ArgumentNullException>(() =>
            obs.Subscribe(null!));
    }

    [TestMethod]
    public void Subscribe_ImmediatelyEmitsCurrentValue()
    {
        var obs = new LolitaObservableString("key", "initial");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.AreEqual(1, received.Count);
        Assert.AreEqual("initial", received[0]);
    }

    [TestMethod]
    public void Subscribe_ImmediatelyEmitsNullCurrentValue()
    {
        var obs = new LolitaObservableString("key", null);
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.AreEqual(1, received.Count);
        Assert.IsNull(received[0]);
    }

    [TestMethod]
    public void Subscribe_MultipleObservers_AllReceiveCurrentValue()
    {
        var obs = new LolitaObservableString("key", "val");
        var countA = 0;
        var countB = 0;
        obs.Subscribe(new DelegateObserver<string?>(_ => countA++));
        obs.Subscribe(new DelegateObserver<string?>(_ => countB++));

        Assert.AreEqual(1, countA);
        Assert.AreEqual(1, countB);
    }

    // ── OnNext ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void OnNext_UpdatesCurrentValue()
    {
        var obs = new LolitaObservableString("key", "initial");
        obs.OnNext("updated");
        Assert.AreEqual("updated", obs.CurrentValue);
    }

    [TestMethod]
    public void OnNext_NotifiesSubscriber()
    {
        var obs = new LolitaObservableString("key", "initial");
        var received = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        obs.OnNext("next");

        Assert.AreEqual(2, received.Count); // initial + next
        Assert.AreEqual("initial", received[0]);
        Assert.AreEqual("next", received[1]);
    }

    [TestMethod]
    public void OnNext_NullValue_UpdatesCurrentValueToNull()
    {
        var obs = new LolitaObservableString("key", "initial");
        obs.OnNext(null);
        Assert.IsNull(obs.CurrentValue);
    }

    [TestMethod]
    public void OnNext_NotifiesMultipleSubscribers()
    {
        var obs = new LolitaObservableString("key", "a");
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        obs.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        obs.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        obs.OnNext("b");

        // each observer got the initial value + the new one
        Assert.AreEqual(2, receivedA.Count);
        Assert.AreEqual("b", receivedA[1]);
        Assert.AreEqual(2, receivedB.Count);
        Assert.AreEqual("b", receivedB[1]);
    }

    [TestMethod]
    public void OnNext_NoSubscribers_DoesNotThrow()
    {
        var obs = new LolitaObservableString("key", "initial");
        obs.OnNext("updated");
        Assert.AreEqual("updated", obs.CurrentValue);
    }

    // ── Dispose / Unsubscribe ────────────────────────────────────────────────

    [TestMethod]
    public void Dispose_StopsReceivingUpdates()
    {
        var obs = new LolitaObservableString("key", "initial");
        var received = new List<string?>();
        var subscription = obs.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        subscription.Dispose();
        obs.OnNext("after-dispose");

        Assert.AreEqual(1, received.Count); // only initial value
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var obs = new LolitaObservableString("key", "initial");
        var subscription = obs.Subscribe(new DelegateObserver<string?>(_ => { }));

        subscription.Dispose();
        subscription.Dispose(); // second dispose must be safe
    }

    [TestMethod]
    public void Dispose_OtherSubscribersStillReceiveUpdates()
    {
        var obs = new LolitaObservableString("key", "initial");
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();

        var subA = obs.Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        obs.Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        subA.Dispose();
        obs.OnNext("next");

        Assert.AreEqual(1, receivedA.Count); // A only got initial
        Assert.AreEqual(2, receivedB.Count); // B got initial + next
    }

    // ── Thread safety ────────────────────────────────────────────────────────

    [TestMethod]
    public void OnNext_ConcurrentSubscribersAndUpdates_DoesNotThrow()
    {
        var obs = new LolitaObservableString("key", "0");

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            var sub = obs.Subscribe(new DelegateObserver<string?>(_ => { }));
            obs.OnNext(i.ToString());
            sub.Dispose();
        })).ToArray();

        Task.WaitAll(tasks);
        // no exception = pass
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
