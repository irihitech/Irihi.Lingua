using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace Irihi.Lingua.Tests;

public class LinguaFormatRegressionTests
{
    [Fact]
    public void KeyTemplate_CultureChange_ReformatsWhenVariantOmitsKey()
    {
        var manager = new FakeLinguaManager()
            .Add(CultureInfo.InvariantCulture, ("Fmt", "Value: {0:F1}"))
            .Add(new CultureInfo("fr-FR"), ("Other", "Autre"));
        var received = new List<string?>();
        using var subscription = manager.Keys("Fmt").CreateFormat().Arg(1.5).Build()
            .Subscribe(new Observer(received.Add));

        manager.UpdateCulture(new CultureInfo("fr-FR"));

        Assert.Equal(new[] { "Value: 1.5", "Value: 1,5" }, received);
    }

    [Fact]
    public void KeyTemplate_CultureAndTemplatePush_EmitIdenticalResultOnlyOnce()
    {
        var manager = new FakeLinguaManager()
            .Add(CultureInfo.InvariantCulture, ("Fmt", "Value: {0:F1}"))
            .Add(new CultureInfo("fr-FR"), ("Fmt", "Value: {0:F1}"));
        var received = new List<string?>();
        using var subscription = manager.Keys("Fmt").CreateFormat().Arg(1.5).Build()
            .Subscribe(new Observer(received.Add));

        manager.UpdateCulture(new CultureInfo("fr-FR"));

        Assert.Equal(new[] { "Value: 1.5", "Value: 1,5" }, received);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentUpdates_NotifyInOrder(bool useRefresh)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = new FormatSource(useRefresh);
        var received = new ConcurrentQueue<string?>();
        using var firstEntered = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var secondStarted = new ManualResetEventSlim();
        using var subscription = source.Formatted.Subscribe(new Observer(value =>
        {
            if (value == "1")
            {
                firstEntered.Set();
                Assert.True(releaseFirst.Wait(TimeSpan.FromSeconds(10), cancellationToken));
            }
            received.Enqueue(value);
        }));

        var first = Task.Run(() => source.Push(1), cancellationToken);
        Task? second = null;
        try
        {
            Assert.True(firstEntered.Wait(TimeSpan.FromSeconds(10), cancellationToken));
            second = Task.Factory.StartNew(() =>
            {
                secondStarted.Set();
                source.Push(2);
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.True(secondStarted.Wait(TimeSpan.FromSeconds(10), cancellationToken));

            // Give the second push time to expose concurrent delivery while the
            // first callback is blocked. A serialized push waits for release.
            await Task.WhenAny(second, Task.Delay(200, cancellationToken));
        }
        finally
        {
            releaseFirst.Set();
        }

        await Task.WhenAll(first, second!).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        Assert.Equal(new[] { "0", "1", "2" }, received);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReentrantUpdate_AllObserversReceiveValuesInOrder(bool useRefresh)
    {
        var source = new FormatSource(useRefresh);
        var first = new List<string?>();
        var second = new List<string?>();
        using var firstSubscription = source.Formatted.Subscribe(new Observer(value =>
        {
            first.Add(value);
            if (value == "1")
                source.Push(2);
        }));
        using var secondSubscription = source.Formatted.Subscribe(new Observer(second.Add));

        source.Push(1);

        Assert.Equal(new[] { "0", "1", "2" }, first);
        Assert.Equal(new[] { "0", "1", "2" }, second);
    }

    [Fact]
    public void ReentrantSubscription_ReceivesInitialValueBeforeLaterUpdates()
    {
        var source = new FormatSource(useRefresh: false);
        var received = new List<string?>();
        IDisposable? nested = null;
        using var subscription = source.Formatted.Subscribe(new Observer(value =>
        {
            if (value == "1")
            {
                nested = source.Formatted.Subscribe(new Observer(received.Add));
                source.Push(2);
            }
        }));

        try
        {
            source.Push(1);
            Assert.Equal(new[] { "1", "2" }, received);
        }
        finally
        {
            nested?.Dispose();
        }
    }

    [Fact]
    public void ReentrantSubscription_DisposedBeforeDelivery_DoesNotNotify()
    {
        var source = new FormatSource(useRefresh: false);
        var received = new List<string?>();
        using var subscription = source.Formatted.Subscribe(new Observer(value =>
        {
            if (value == "1")
                source.Formatted.Subscribe(new Observer(received.Add)).Dispose();
        }));

        source.Push(1);
        source.Push(2);

        Assert.Empty(received);
    }

    [Fact]
    public void InitialObserverThrows_SubscriptionIsRemoved()
    {
        var source = new FormatSource(useRefresh: false);
        Assert.Throws<InvalidOperationException>(() => source.Formatted.Subscribe(
            new Observer(_ => throw new InvalidOperationException("Initial callback failed."))));

        var received = new List<string?>();
        using var subscription = source.Formatted.Subscribe(new Observer(received.Add));
        source.Push(1);

        Assert.Equal(new[] { "0", "1" }, received);
    }

    [Fact]
    public async Task PropertyChange_DuringInitialRead_IsNotLost()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var model = new PropertyModel { Value = 1 };
        using var readStarted = new ManualResetEventSlim();
        using var releaseRead = new ManualResetEventSlim();
        using var valueChanged = new ManualResetEventSlim();
        var reads = 0;
        var formatted = new FakeLinguaManager().CreateFormat("{0}")
            .Arg(model, nameof(PropertyModel.Value), m =>
            {
                var value = m.Value;
                if (Interlocked.Increment(ref reads) == 1)
                {
                    readStarted.Set();
                    Assert.True(releaseRead.Wait(TimeSpan.FromSeconds(10), cancellationToken));
                }
                return value;
            }).Build();
        var received = new ConcurrentQueue<string?>();

        var subscribing = Task.Run(() => formatted.Subscribe(new Observer(received.Enqueue)), cancellationToken);
        Task? changing = null;
        try
        {
            Assert.True(readStarted.Wait(TimeSpan.FromSeconds(10), cancellationToken));
            changing = Task.Run(() =>
            {
                model.SetSilently(2);
                valueChanged.Set();
                model.Raise(nameof(PropertyModel.Value));
            }, cancellationToken);
            Assert.True(valueChanged.Wait(TimeSpan.FromSeconds(10), cancellationToken));
        }
        finally
        {
            releaseRead.Set();
        }

        using var subscription = await subscribing.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        await changing!.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        Assert.Equal("2", received.Last());
    }

    [Theory]
    [InlineData("Value")]
    [InlineData(null)]
    [InlineData("")]
    public void PropertyChange_OnlyReadsTheNotifyingSource(string? propertyName)
    {
        var first = new PropertyModel { Value = 1 };
        var second = new PropertyModel { Value = 10 };
        var secondReads = 0;
        var received = new List<string?>();
        using var subscription = new FakeLinguaManager().CreateFormat("{0}/{1}")
            .Arg(first, nameof(PropertyModel.Value), m => m.Value)
            .Arg(second, nameof(PropertyModel.Value), m =>
            {
                secondReads++;
                return m.Value;
            }).Build().Subscribe(new Observer(received.Add));

        second.SetSilently(20);
        first.SetSilently(2);
        first.Raise(propertyName);

        Assert.Equal(1, secondReads);
        Assert.Equal(new[] { "1/10", "2/10" }, received);

        second.Raise(nameof(PropertyModel.Value));
        Assert.Equal("2/20", received.Last());
    }

    [Fact]
    public void PropertySources_WithValueEquality_AreBothSubscribedAndDetached()
    {
        var first = new EqualPropertyModel { Value = 1 };
        var second = new EqualPropertyModel { Value = 10 };
        var reads = 0;
        var received = new List<string?>();
        var formatted = new FakeLinguaManager().CreateFormat("{0}/{1}")
            .Arg(first, nameof(PropertyModel.Value), m => m.Value)
            .Arg(second, nameof(PropertyModel.Value), m =>
            {
                reads++;
                return m.Value;
            }).Build();
        var subscription = formatted.Subscribe(new Observer(received.Add));

        second.Value = 20;
        Assert.Equal(new[] { "1/10", "1/20" }, received);

        subscription.Dispose();
        second.Value = 30;
        Assert.Equal(2, reads);
    }

    [Fact]
    public void InitialPropertyGetterThrows_DetachesEventsAndAllowsRetry()
    {
        var model = new PropertyModel { Value = 1 };
        var reads = 0;
        var formatted = new FakeLinguaManager().CreateFormat("{0}")
            .Arg(model, nameof(PropertyModel.Value), m =>
            {
                if (++reads == 1)
                    throw new InvalidOperationException("First read failed.");
                return m.Value;
            }).Build();

        Assert.Throws<InvalidOperationException>(() => formatted.Subscribe(new Observer(_ => { })));
        model.Value = 2;
        Assert.Equal(1, reads);

        var received = new List<string?>();
        using var subscription = formatted.Subscribe(new Observer(received.Add));
        model.Value = 3;
        Assert.Equal(new[] { "2", "3" }, received);
    }

    private sealed class FormatSource
    {
        private readonly bool _useRefresh;
        private readonly LinguaObservable<int> _value = new("value", 0);
        private int _current;

        public LinguaFormatObservable Formatted { get; }

        public FormatSource(bool useRefresh)
        {
            _useRefresh = useRefresh;
            var builder = new FakeLinguaManager().CreateFormat("{0}");
            Formatted = (useRefresh
                ? builder.Arg(() => Volatile.Read(ref _current))
                : builder.Arg(_value)).Build();
        }

        public void Push(int value)
        {
            if (_useRefresh)
            {
                Volatile.Write(ref _current, value);
                Formatted.Refresh();
            }
            else
            {
                _value.OnNext(value);
            }
        }
    }

    private class PropertyModel : INotifyPropertyChanged
    {
        private int _value;
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Value
        {
            get => Volatile.Read(ref _value);
            set
            {
                SetSilently(value);
                Raise(nameof(Value));
            }
        }

        public void SetSilently(int value) => Volatile.Write(ref _value, value);
        public void Raise(string? propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class EqualPropertyModel : PropertyModel
    {
        public override bool Equals(object? obj) => obj is EqualPropertyModel;
        public override int GetHashCode() => 0;
    }

    private sealed class Observer(Action<string?> next) : IObserver<string?>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(string? value) => next(value);
    }
}
