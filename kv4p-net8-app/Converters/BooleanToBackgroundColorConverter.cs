using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace kv4p_net8_app.Converters;

public class BooleanToBackgroundColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Brushes.LightGreen : Brushes.PaleVioletRed;
        }
        return Brushes.LightGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException(
            "ConvertBack is not supported in BooleanToBackgroundColorConverter."
        );
    }
}
