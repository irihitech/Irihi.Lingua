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

        var args = values.Skip(1).ToArray();
        return string.Format(culture, format, args);
    }
}