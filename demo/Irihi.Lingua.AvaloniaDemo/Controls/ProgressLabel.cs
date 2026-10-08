using Avalonia;
using Avalonia.Controls;
using Irihi.Lingua;

namespace Irihi.Lingua.AvaloniaDemo.Controls;

/// <summary>
/// Demonstrates an Avalonia <see cref="DirectProperty{TOwner,TValue}"/>
/// participating in a Lingua <c>Format</c> chain: the control's own
/// <see cref="Progress"/> property (a DirectProperty) feeds the localized
/// <c>Progress_Template</c>, and the formatted result is pushed into the
/// inherited <see cref="TextBlock.Text"/> property.
/// </summary>
public class ProgressLabel : TextBlock
{
    public static readonly DirectProperty<ProgressLabel, double> ProgressProperty =
        AvaloniaProperty.RegisterDirect<ProgressLabel, double>(
            nameof(Progress), o => o.Progress, (o, v) => o.Progress = v);

    private double _progress;
    private IDisposable? _subscription;

    public double Progress
    {
        get => _progress;
        set => SetAndRaise(ProgressProperty, ref _progress, value);
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // GetObservable on a DirectProperty yields an IObservable<double> with
        // behavior-subject semantics (current value immediately, then every
        // change) — it plugs straight into the Arg overload.
        _subscription = LanguageManager.Keys.Progress_Template.Format()
            .Arg(this.GetObservable(ProgressProperty))
            .Build().Subscribe(new DelegateObserver<string?>(v => Text = v));
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _subscription?.Dispose();
        _subscription = null;
    }

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
