using System.Collections;
using System.Globalization;

namespace DogWalkerApp.Converters;

public class CollectionEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
