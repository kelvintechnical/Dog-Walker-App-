using System.Globalization;
using DogWalker.Core.Enums;

namespace DogWalkerApp.Converters;

public class MessageAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageDirection direction)
        {
            return direction == MessageDirection.WalkerToClient
                ? LayoutOptions.End
                : LayoutOptions.Start;
        }

        return LayoutOptions.Start;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
