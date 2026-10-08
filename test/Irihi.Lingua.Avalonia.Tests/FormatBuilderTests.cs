using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

/// <summary>
/// Verifies that observables over Avalonia properties — including
/// DirectProperties — participate in the code-side Format chain.
/// </summary>
public class FormatBuilderTests
{
    [AvaloniaFact]
    public void FormatBuilder_DirectPropertyArgument_UpdatesOnPropertyAndCultureChanges()
    {
        TestLanguageManager.Instance.Reset();

        var spinner = new TestSpinner { Value = 1.5 };
        var received = new List<string?>();
        TestLanguageManager.Keys.Format_Template.CreateFormat()
            .Arg(TestLanguageManager.Instance.Greeting_Message)
            .Arg(spinner.GetObservable(TestSpinner.ValueProperty))
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Hello, Page 1.5", received[0]);

        // Both the template and the Greeting_Message argument are manager keys;
        // a culture switch pushes them one after another, so a transient mix
        // (new greeting + old template) precedes the final value. UI bindings
        // coalesce same-tick pushes — assert the final value.
        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Assert.Equal("你好, 第1.5页", received[^1]);

        spinner.Value = 2.5;
        Assert.Equal("你好, 第2.5页", received[^1]);
    }

    [AvaloniaFact]
    public void FormatBuilder_StyledPropertyArgument_AlsoWorks()
    {
        TestLanguageManager.Instance.Reset();

        // NumericUpDown.Value is a StyledProperty.
        var numeric = new NumericUpDown { Value = 3m };
        var received = new List<string?>();
        TestLanguageManager.Keys.Format_Template.CreateFormat()
            .Arg(TestLanguageManager.Instance.Greeting_Message)
            .Arg(numeric.GetObservable(NumericUpDown.ValueProperty))
            .Build().Subscribe(new DelegateObserver<string?>(v => received.Add(v)));

        Assert.Equal("Hello, Page 3", received[0]);

        numeric.Value = 7m;
        Assert.Equal("Hello, Page 7", received[^1]);
    }

    private sealed class TestSpinner : Control
    {
        public static readonly DirectProperty<TestSpinner, double> ValueProperty =
            AvaloniaProperty.RegisterDirect<TestSpinner, double>(
                nameof(Value), o => o.Value, (o, v) => o.Value = v);

        private double _value;

        public double Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }
    }

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
