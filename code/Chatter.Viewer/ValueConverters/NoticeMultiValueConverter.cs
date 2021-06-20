using Avalonia.Data.Converters;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace Chatter.Viewer.ValueConverters
{
    internal class NoticeMultiValueConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var status = values.OfType<ProfileOnlineStatus>().SingleOrDefault();
            var address = values.OfType<IPAddress>().SingleOrDefault();
            if (address is null || status != ProfileOnlineStatus.Online)
                return "[Offline]";
            return $"[{address}]";
        }
    }
}
