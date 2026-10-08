using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Irihi.Lingua.Extensions;

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

        // When fed by FormatTranslateExtension, the last value is the owning
        // manager's active culture.  Use it for string.Format so number and
        // date formatting follows the language selected through the manager
        // instead of the thread culture.
        var formatCulture = culture;
        var argCount = values.Count - 1;
        if (argCount > 0 && values[^1] is CultureInfo managerCulture)
        {
            formatCulture = managerCulture;
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
