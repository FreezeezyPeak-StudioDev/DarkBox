using System.Globalization;
using System.Windows.Data;

namespace DarkBox.App.Converters;

public sealed class PasswordDisplayConverter : IMultiValueConverter
{
    private const string Mask = "••••••••";

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var password = values.Length > 0 ? values[0] as string : null;

        if (string.IsNullOrEmpty(password))
            return string.Empty;

        var isVisible = values.Length > 1 && values[1] is true;
        return isVisible ? password : Mask;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
