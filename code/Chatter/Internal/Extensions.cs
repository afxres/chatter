using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chatter.Internal
{
    internal static class Extensions
    {
        public static T FindAncestor<T>(this FrameworkElement element) where T : DependencyObject
        {
            var parent = default(DependencyObject);
            while (element != null && (parent = VisualTreeHelper.GetParent(element)) != null && parent is T == false)
                element = parent as FrameworkElement;
            return parent as T;
        }

        public static T FindChild<T>(this DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null)
                return null;
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var item = VisualTreeHelper.GetChild(parent, i);
                if (item is T element && Equals(element.Name, name) || (element = FindChild<T>(item, name)) != null)
                    return element;
            }
            return null;
        }

        public static void Insert(this TextBox textbox, string text)
        {
            if (text == null)
                return;
            var current = textbox.Text ?? string.Empty;
            var start = textbox.SelectionStart;
            var length = textbox.SelectionLength;
            var before = current.Substring(0, start);
            var after = current.Substring(start + length);
            var result = string.Concat(before, text, after);
            textbox.Text = result;
            textbox.SelectionStart = start + text.Length;
            textbox.SelectionLength = 0;
        }

        public static string ToUnit(long length)
        {
            return ToUnit(length, out var result, out var unit) ? $"{result:0.00} {unit}B" : string.Empty;
        }

        public static bool ToUnit(long length, out double result, out string unit)
        {
            var units = new[] { string.Empty, "K", "M", "G", "T", "P", "E" };
            if (length < 0)
                goto fail;
            var tmp = length;
            var idx = 0;
            while (idx < units.Length - 1)
            {
                if (tmp < 1024)
                    break;
                tmp >>= 10;
                idx++;
            }
            result = length / Math.Pow(1024, idx);
            unit = units[idx];
            return true;

        fail:
            result = 0;
            unit = string.Empty;
            return false;
        }
    }
}
