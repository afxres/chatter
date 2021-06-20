using Mikodev.Links.Abstractions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Chatter.Converters
{
    internal sealed class MessageStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is MessageStatus status && status != default ? status.ToString() : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
