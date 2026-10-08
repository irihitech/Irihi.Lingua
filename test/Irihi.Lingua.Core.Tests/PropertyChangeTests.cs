using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

public class PropertyChangeTests
{
    private static FakeLinguaManager CreateManager() => new FakeLinguaManager()
        .Add(CultureInfo.InvariantCulture, ("Fmt", "Page {0} of {1}"))
        .Add(new CultureInfo("zh-Hans"), ("Fmt", "第{0}页 共{1}页"));

    // ── Live property arguments through Format ───────────────────────────────

    [Fact]
    public void Format_PropertyArgument_EmitsCurrentPropertyValueImmediately()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 3, TotalPages = 10 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        var single = Assert.Single(received);
        Assert.Equal("Page 3 of 10", single);
    }

    [Fact]
    public void Format_PropertyArgument_ChangeTriggersReformat()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(model, nameof(TestModel.TotalPages), s => s.TotalPages)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Page = 4;
        model.TotalPages = 20;

        Assert.Equal(
        [
            "Page 1 of 10",  // initial
            "Page 4 of 10",  // Page changed
            "Page 4 of 20",  // TotalPages changed
        ], received);
    }

    [Fact]
    public void Format_PropertyArgument_OtherPropertyChanged_DoesNotReformat()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Name = "unrelated";

        Assert.Single(received);
    }

    [Fact]
    public void Format_PropertyArgument_NullOrEmptyPropertyName_MatchesAnyProperty()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Page = 2;       // normal raise
        model.Raise(null);    // "everything changed" raise
        model.Raise(string.Empty);

        Assert.Equal(new[] { "Page 1 of 10", "Page 2 of 10" }, received);
    }

    [Fact]
    public void Format_PropertyArgument_SetToSameValue_DoesNotReEmit()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Page = 1; // setter raises even though the value is the same

        Assert.Single(received);
    }

    [Fact]
    public void Format_PropertyArgument_ReferenceTypeProperty_Works()
    {
        var manager = new FakeLinguaManager()
            .Add(CultureInfo.InvariantCulture, ("Fmt", "Hello {0}"));
        var model = new TestModel { Name = "abc" };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Name), s => s.Name)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Name = "xyz";

        Assert.Equal(new[] { "Hello abc", "Hello xyz" }, received);
    }

    // ── Subscription lifetime ────────────────────────────────────────────────

    [Fact]
    public void Format_PropertyArgument_Dispose_StopsReceivingUpdates()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10);
        var received = new List<string?>();
        var subscription = formatted.Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        subscription.Dispose();
        model.Page = 9;

        Assert.Single(received);
    }

    [Fact]
    public void Format_PropertyArgument_ResubscribeAfterAllUnsubscribed_RewiresEventAndSnapshots()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10);
        var first = new List<string?>();
        var subscription = formatted.Build().Subscribe(new DelegateObserver<string?>(v => first.Add(v)));
        subscription.Dispose();

        model.Page = 7; // nobody listening

        var second = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => second.Add(v)));
        model.Page = 8;

        Assert.Single(first);
        Assert.Equal(new[] { "Page 7 of 10", "Page 8 of 10" }, second);
    }

    [Fact]
    public void Format_PropertyArgument_MultipleObservers_AllReceiveUpdates()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var formatted = manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(10);
        var receivedA = new List<string?>();
        var receivedB = new List<string?>();
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => receivedA.Add(v)));
        formatted.Build().Subscribe(new DelegateObserver<string?>(v => receivedB.Add(v)));

        model.Page = 2;

        Assert.Equal(2, receivedA.Count);
        Assert.Equal(2, receivedB.Count);
        Assert.Equal("Page 2 of 10", receivedA[1]);
        Assert.Equal("Page 2 of 10", receivedB[1]);
    }

    [Fact]
    public void Format_PropertyArgument_MixedWithConstantAndObservable()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 2, TotalPages = 10 };
        var extra = new LinguaObservable<int>("extra", 5);

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(extra)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        model.Page = 3;
        extra.OnNext(6);

        Assert.Equal(
        [
            "Page 2 of 5",
            "Page 3 of 5",
            "Page 3 of 6",
        ], received);
    }

    // ── Culture interplay ────────────────────────────────────────────────────

    [Fact]
    public void Format_PropertyArgument_PropertyAndCultureChangesBothReformat()
    {
        var manager = CreateManager();
        var model = new TestModel { Page = 1, TotalPages = 10 };

        var received = new List<string?>();
        manager.Keys("Fmt").CreateFormat()
            .Arg(model, nameof(TestModel.Page), s => s.Page)
            .Arg(model, nameof(TestModel.TotalPages), s => s.TotalPages)
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

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

    // ── Argument validation ──────────────────────────────────────────────────

    [Fact]
    public void Format_PropertyArgument_NullSource_ThrowsArgumentNullException()
    {
        var manager = CreateManager();
        Assert.Throws<ArgumentNullException>(() =>
            manager.Keys("Fmt").CreateFormat().Arg<TestModel>(null!, nameof(TestModel.Page), s => s.Page));
    }

    [Fact]
    public void Format_PropertyArgument_EmptyPropertyName_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var model = new TestModel();
        Assert.Throws<ArgumentException>(() =>
            manager.Keys("Fmt").CreateFormat().Arg(model, "", s => s.Page));
    }

    [Fact]
    public void Format_PropertyArgument_NullGetter_ThrowsArgumentNullException()
    {
        var manager = CreateManager();
        var model = new TestModel();
        Assert.Throws<ArgumentNullException>(() =>
            manager.Keys("Fmt").CreateFormat().Arg(model, nameof(TestModel.Page), null!));
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
