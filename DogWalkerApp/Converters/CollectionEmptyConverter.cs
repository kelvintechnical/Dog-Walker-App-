using System;
using System.Collections;
using System.Globalization;

namespace DogWalkerApp.Converters;

public class CollectionEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = IsCollectionEmpty(value);

        if (IsInvert(parameter))
        {
            return !isEmpty;
        }

        return isEmpty;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool IsCollectionEmpty(object? value)
    {
        if (value is IEnumerable collection)
        {
            foreach (var _ in collection)
            {
                return false;
            }

            return true;
        }

        return true;
    }

    private static bool IsInvert(object? parameter) =>
        parameter is string text && text.Equals("invert", StringComparison.OrdinalIgnoreCase);
}
