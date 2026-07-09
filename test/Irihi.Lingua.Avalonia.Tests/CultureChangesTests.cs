using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

/// <summary>
/// Tests that <see cref="ILinguaManager.CultureChanges"/> emits with
/// behavior-subject semantics — initial emission on subscribe and subsequent
/// notifications on <see cref="ILinguaManager.UpdateCulture"/>.
/// </summary>
/// <remarks>
/// <see cref="TestLanguageManager.Instance"/> is a static singleton shared
/// across all test classes.  Parallel test-class execution may trigger
/// concurrent <c>OnNext</c> calls on the same subscriber.  All assertions
/// use thread-safe primitives (<c>??=</c>, <c>Interlocked</c>) rather than
/// <c>List&lt;T&gt;</c> to avoid "collection modified during enumeration".
/// </remarks>
public class CultureChangesTests
{
    [Fact]
    public void CultureChanges_InitialValueIsInvariantCulture()
    {
        TestLanguageManager.Instance.Reset();

        CultureInfo? firstCulture = null;
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v => firstCulture ??= v));

        Assert.NotNull(firstCulture);
        Assert.Same(CultureInfo.InvariantCulture, firstCulture);
    }

    [Fact]
    public void CultureChanges_EmitsOnUpdateCulture()
    {
        TestLanguageManager.Instance.Reset();

        CultureInfo? initialCulture = null;
        CultureInfo? updatedCulture = null;
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v =>
            {
                initialCulture ??= v;
                if (initialCulture is not null && !ReferenceEquals(v, initialCulture))
                    updatedCulture ??= v;
            }));

        var zhHans = new CultureInfo("zh-Hans");
        TestLanguageManager.Instance.UpdateCulture(zhHans);

        Assert.Same(CultureInfo.InvariantCulture, initialCulture);
        Assert.Same(zhHans, updatedCulture);
    }

    [Fact]
    public void CultureChanges_EmitsEvenWhenSameCulturePassedAgain()
    {
        TestLanguageManager.Instance.Reset();

        var notifyCount = 0;
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(_ => Interlocked.Increment(ref notifyCount)));

        var countBefore = Volatile.Read(ref notifyCount);

        var zhHans = new CultureInfo("zh-Hans");
        TestLanguageManager.Instance.UpdateCulture(zhHans);
        TestLanguageManager.Instance.UpdateCulture(zhHans);
        TestLanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);

        Assert.True(Volatile.Read(ref notifyCount) >= countBefore + 3,
            $"Expected at least {countBefore + 3} notifications, got {Volatile.Read(ref notifyCount)}");
    }

    [Fact]
    public void CultureChanges_MultipleSubscribers_AllNotified()
    {
        TestLanguageManager.Instance.Reset();

        var countA = 0;
        var countB = 0;
        using var subA = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(_ => Interlocked.Increment(ref countA)));
        using var subB = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(_ => Interlocked.Increment(ref countB)));

        var beforeA = Volatile.Read(ref countA);
        var beforeB = Volatile.Read(ref countB);
        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.True(Volatile.Read(ref countA) > beforeA, "Subscriber A must receive at least one notification");
        Assert.True(Volatile.Read(ref countB) > beforeB, "Subscriber B must receive at least one notification");
    }

    [Fact]
    public void CultureChanges_DisposedSubscriberNotNotified()
    {
        TestLanguageManager.Instance.Reset();

        var notifyCount = 0;
        var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(_ => Interlocked.Increment(ref notifyCount)));

        sub.Dispose();
        var countBefore = Volatile.Read(ref notifyCount);
        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

        Assert.Equal(countBefore, Volatile.Read(ref notifyCount));
    }

    [Fact]
    public void CultureChanges_NullCulture_TreatsAsInvariant()
    {
        TestLanguageManager.Instance.Reset();

        CultureInfo? initialCulture = null;
        CultureInfo? afterNullCulture = null;
        using var sub = TestLanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(v =>
            {
                if (initialCulture is null)
                {
                    initialCulture = v;
                }
                else if (afterNullCulture is null)
                {
                    afterNullCulture = v;
                }
            }));

        TestLanguageManager.Instance.UpdateCulture(null!);

        Assert.Same(CultureInfo.InvariantCulture, initialCulture);
        Assert.NotNull(afterNullCulture);
        Assert.Same(CultureInfo.InvariantCulture, afterNullCulture);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
