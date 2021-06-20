using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Chatter.Viewer.ValueConverters
{
    internal class IntegerToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int number)
                return number > 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
