using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

/// <summary>
/// Tests that <see cref="ILinguaManager.CultureChanges"/> emits with
/// behavior-subject semantics — initial emission on subscribe and subsequent
/// notifications on <see cref="ILinguaManager.UpdateCulture"/>.
/// </summary>
public class CultureChangesTests
{
    [Fact]
    public void CultureChanges_InitialValueIsInvariantCulture()
    {
        TestLanguageManager.Instance.Reset();

        var received = new List<CultureInfo>();
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Same(CultureInfo.InvariantCulture, single);
    }

    [Fact]
    public void CultureChanges_EmitsOnUpdateCulture()
    {
        TestLanguageManager.Instance.Reset();

        var received = new List<CultureInfo>();
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v => received.Add(v)));

        var zhHans = new CultureInfo("zh-Hans");
        TestLanguageManager.Instance.UpdateCulture(zhHans);

        Assert.Equal(2, received.Count);
        Assert.Same(CultureInfo.InvariantCulture, received[0]);
        Assert.Same(zhHans, received[1]);
    }

    [Fact]
    public void CultureChanges_EmitsEvenWhenSameCulturePassedAgain()
    {
        TestLanguageManager.Instance.Reset();

        var received = new List<CultureInfo>();
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v => received.Add(v)));

        var zhHans = new CultureInfo("zh-Hans");
        TestLanguageManager.Instance.UpdateCulture(zhHans);
        TestLanguageManager.Instance.UpdateCulture(zhHans);
        TestLanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);

        Assert.Equal(4, received.Count); // initial + 3 updates
    }

    [Fact]
    public void CultureChanges_MultipleSubscribers_AllNotified()
    {
        TestLanguageManager.Instance.Reset();

        var countA = 0;
        var countB = 0;
        using var subA = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(_ => countA++));
        using var subB = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(_ => countB++));

        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.Equal(2, countA); // initial + update
        Assert.Equal(2, countB);
    }

    [Fact]
    public void CultureChanges_DisposedSubscriberNotNotified()
    {
        TestLanguageManager.Instance.Reset();

        var received = new List<CultureInfo>();
        var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v => received.Add(v)));

        sub.Dispose();
        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.Single(received); // only initial value
    }

    [Fact]
    public void CultureChanges_NullCulture_TreatsAsInvariant()
    {
        TestLanguageManager.Instance.Reset();

        var received = new List<CultureInfo>();
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v => received.Add(v)));

        TestLanguageManager.Instance.UpdateCulture(null!);

        Assert.Equal(2, received.Count);
        Assert.Same(CultureInfo.InvariantCulture, received[1]);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
