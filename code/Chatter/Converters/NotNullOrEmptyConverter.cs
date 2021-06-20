using System;
using System.Globalization;
using System.Windows.Data;

namespace Chatter.Converters
{
    internal sealed class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var empty = value is string data
                ? string.IsNullOrEmpty(data)
                : value is null;
            return !empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
