using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

public class NotifyPropertyChangedExtensionsTests
{
    // ── Initial emission ─────────────────────────────────────────────────────

    [Fact]
    public void ObserveProperty_EmitsCurrentValueImmediatelyOnSubscribe()
    {
        var model = new TestModel { Page = 3 };

        var received = new List<int?>();
        model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal(3, single);
    }

    [Fact]
    public void ObserveProperty_ReferenceTypeProperty_EmitsCurrentValue()
    {
        var model = new TestModel { Name = "abc" };

        var received = new List<string?>();
        model.ObserveProperty(nameof(TestModel.Name), () => model.Name)
            .Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("abc", single);
    }

    // ── Change notification ──────────────────────────────────────────────────

    [Fact]
    public void ObserveProperty_PropertyChanged_PushesNewValue()
    {
        var model = new TestModel { Page = 1 };

        var received = new List<int?>();
        model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        model.Page = 5;

        Assert.Equal(2, received.Count);
        Assert.Equal(1, received[0]);
        Assert.Equal(5, received[1]);
    }

    [Fact]
    public void ObserveProperty_OtherPropertyChanged_DoesNotPush()
    {
        var model = new TestModel();

        var received = new List<int?>();
        model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        model.Name = "other";

        Assert.Single(received);
    }

    [Fact]
    public void ObserveProperty_NullOrEmptyPropertyName_MatchesAnyProperty()
    {
        var model = new TestModel { Page = 1 };

        var received = new List<int?>();
        model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        model.Page = 2; // normal raise
        model.Raise(null); // "everything changed" raise
        model.Raise(string.Empty);

        Assert.Equal(new int?[] { 1, 2 }, received);
    }

    [Fact]
    public void ObserveProperty_IdenticalValue_IsNotReEmitted()
    {
        var model = new TestModel { Page = 1 };

        var received = new List<int?>();
        model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        model.Page = 1; // setter raises even though the value is the same

        Assert.Single(received);
    }

    // ── Subscription lifetime ────────────────────────────────────────────────

    [Fact]
    public void ObserveProperty_Dispose_StopsReceivingUpdates()
    {
        var model = new TestModel { Page = 1 };

        var received = new List<int?>();
        var subscription = model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(v => received.Add(v)));

        subscription.Dispose();
        model.Page = 9;

        Assert.Single(received);
    }

    [Fact]
    public void ObserveProperty_Dispose_CalledTwice_DoesNotThrow()
    {
        var model = new TestModel();

        var subscription = model.ObserveProperty(nameof(TestModel.Page), () => model.Page)
            .Subscribe(new DelegateObserver<int>(_ => { }));

        subscription.Dispose();
        subscription.Dispose();
    }

    [Fact]
    public void ObserveProperty_ResubscribeAfterAllUnsubscribed_ReattachesToSource()
    {
        var model = new TestModel { Page = 1 };

        var observable = model.ObserveProperty(nameof(TestModel.Page), () => model.Page);
        var first = new List<int?>();
        var subscription = observable.Subscribe(new DelegateObserver<int>(v => first.Add(v)));
        subscription.Dispose();

        model.Page = 7; // nobody listening

        var second = new List<int?>();
        observable.Subscribe(new DelegateObserver<int>(v => second.Add(v)));

        var single = Assert.Single(second);
        Assert.Equal(7, single);
    }

    [Fact]
    public void ObserveProperty_MultipleObservers_AllReceiveUpdates()
    {
        var model = new TestModel { Page = 1 };

        var receivedA = new List<int?>();
        var receivedB = new List<int?>();
        var observable = model.ObserveProperty(nameof(TestModel.Page), () => model.Page);
        observable.Subscribe(new DelegateObserver<int>(v => receivedA.Add(v)));
        observable.Subscribe(new DelegateObserver<int>(v => receivedB.Add(v)));

        model.Page = 2;

        Assert.Equal(2, receivedA.Count);
        Assert.Equal(2, receivedB.Count);
    }

    [Fact]
    public void ObserveProperty_DisposeOneObserver_OtherStillReceivesUpdates()
    {
        var model = new TestModel { Page = 1 };

        var observable = model.ObserveProperty(nameof(TestModel.Page), () => model.Page);
        var receivedA = new List<int?>();
        var receivedB = new List<int?>();
        var subA = observable.Subscribe(new DelegateObserver<int>(v => receivedA.Add(v)));
        observable.Subscribe(new DelegateObserver<int>(v => receivedB.Add(v)));

        subA.Dispose();
        model.Page = 2;

        Assert.Single(receivedA);
        Assert.Equal(2, receivedB.Count);
    }

    // ── Argument validation ──────────────────────────────────────────────────

    [Fact]
    public void ObserveProperty_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NotifyPropertyChangedExtensions.ObserveProperty(null!, nameof(TestModel.Page), () => 1));
    }

    [Fact]
    public void ObserveProperty_EmptyPropertyName_ThrowsArgumentException()
    {
        var model = new TestModel();
        Assert.Throws<ArgumentException>(() =>
            model.ObserveProperty("", () => model.Page));
    }

    [Fact]
    public void ObserveProperty_NullGetter_ThrowsArgumentNullException()
    {
        var model = new TestModel();
        Assert.Throws<ArgumentNullException>(() =>
            model.ObserveProperty<int>(nameof(TestModel.Page), null!));
    }

    // ── End-to-end with Format ───────────────────────────────────────────────

    [Fact]
    public void ObserveProperty_CombinedWithFormat_PropertyAndCultureChangesBothReformat()
    {
        var manager = new FakeLinguaManager()
            .Add(CultureInfo.InvariantCulture, ("Fmt", "Page {0} of {1}"))
            .Add(new CultureInfo("zh-Hans"), ("Fmt", "第{0}页 共{1}页"));
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var formatted = manager.Keys("Fmt").Format(
            model.ObserveProperty(nameof(TestModel.Page), () => model.Page),
            model.ObserveProperty(nameof(TestModel.TotalPages), () => model.TotalPages));

        var received = new List<string?>();
        formatted.Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Page = 4;
        manager.UpdateCulture(new CultureInfo("zh-Hans"));
        model.TotalPages = 20;

        Assert.Equal(
        [
            "Page 1 of 10",   // initial
            "Page 4 of 10",   // Page changed
            "第4页 共10页",    // culture changed
            "第4页 共20页",    // TotalPages changed
        ], received);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }

    private sealed class TestModel : INotifyPropertyChanged
    {
        private int _page;
        private int _totalPages;
        private string? _name;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Page
        {
            get => _page;
            set
            {
                _page = value;
                Raise(nameof(Page));
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                Raise(nameof(TotalPages));
            }
        }

        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                Raise(nameof(Name));
            }
        }

        public void Raise(string? propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
