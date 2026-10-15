using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Irihi.Lingua.Extensions;

/// <summary>
/// The internal marker used by <see cref="FormatTranslateExtension"/> to feed
/// the built-in converter the manager's active culture through the trailing
/// binding.  Being a dedicated type, it never collides with user-supplied
/// <see cref="CultureInfo"/> format arguments.
/// </summary>
internal readonly record struct CultureSignal(CultureInfo Culture);

/// <summary>
/// Adapts the manager's <see cref="IObservable{T}"/> of culture into an
/// observable of <see cref="CultureSignal"/> for the trailing binding.
/// </summary>
internal sealed class CultureSignalAdapter(IObservable<CultureInfo> source) : IObservable<CultureSignal>
{
    public IDisposable Subscribe(IObserver<CultureSignal> observer) =>
        source.Subscribe(new Adapter(observer));

    private sealed class Adapter(IObserver<CultureSignal> inner) : IObserver<CultureInfo>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(CultureInfo value) => inner.OnNext(new CultureSignal(value));
    }
}

public sealed class FormatTranslateConverter: IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0)
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (values[0] is not string format)
        {
            return AvaloniaProperty.UnsetValue;
        }

        // When fed by FormatTranslateExtension, the last value is an internal
        // CultureSignal carrying the owning manager's active culture.  Use it
        // for string.Format so number and date formatting follows the language
        // selected through the manager instead of the thread culture.
        var formatCulture = culture;
        var argCount = values.Count - 1;
        if (argCount > 0 && values[^1] is CultureSignal signal)
        {
            formatCulture = signal.Culture;
            argCount--;
        }

        if (argCount == 0)
        {
            return string.Format(formatCulture, format);
        }

        var args = new object?[argCount];
        for (var i = 0; i < argCount; i++)
        {
            args[i] = values[i + 1];
        }

        return string.Format(formatCulture, format, args);
    }
}
