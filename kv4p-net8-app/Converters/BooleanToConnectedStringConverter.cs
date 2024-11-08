using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace kv4p_net8_app.Converters;

public class BooleanToConnectedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Connected" : "Not Connected";
        }
        return "Invalid binding";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException(
            "ConvertBack is not supported in BooleanToBackgroundColorConverter."
        );
    }
}
