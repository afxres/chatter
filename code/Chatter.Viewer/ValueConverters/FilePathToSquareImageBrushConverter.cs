using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Mikodev.Optional;
using System;
using System.Globalization;
using System.IO;
using static Mikodev.Optional.Extensions;

namespace Chatter.Viewer.ValueConverters
{
    internal class FilePathToSquareImageBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            static MemoryStream HandleImage(string path)
            {
                using var source = new System.Drawing.Bitmap(path);
                var h = source.Height;
                var w = source.Width;
                var m = Math.Min(h, w);
                var x = w == m ? 0 : (w - m) / 2;
                var y = h == m ? 0 : (h - m) / 2;
                var target = new System.Drawing.Bitmap(m, m);
                using var g = System.Drawing.Graphics.FromImage(target);
                g.DrawImage(source, new System.Drawing.Rectangle(0, 0, m, m), new System.Drawing.Rectangle(x, y, m, m), System.Drawing.GraphicsUnit.Pixel);
                var stream = new MemoryStream();
                target.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Position = default;
                return stream;
            }

            if (value is string path && !string.IsNullOrEmpty(path))
                return Try(() => new ImageBrush(new Bitmap(HandleImage(path))) as IBrush).UnwrapOr(Brushes.LightGray);
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
