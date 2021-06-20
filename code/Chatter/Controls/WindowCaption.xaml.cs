using Chatter.Internal;
using Chatter.Interop;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chatter.Controls
{
    public partial class WindowCaption : UserControl
    {
        private const string IsTabletModeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ImmersiveShell";

        private const string IsTabletModeValue = "TabletMode";

        public WindowCaption()
        {
            this.InitializeComponent();
            Loaded += this.UserControl_Loaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var window = this.FindAncestor<Window>();
            if (window != null)
            {
                _ = NativeMethods.EnableAcrylicBlur(window);
                _ = NativeMethods.DisableSystemMenu(window);

                window.SizeChanged += this.Window_Changed;
                window.StateChanged += this.Window_Changed;
            }
        }

        private void Window_Changed(object sender, EventArgs e)
        {
            var window = (Window)sender;
            var grid = window.FindChild<Grid>("WindowGrid");
            var border = window.FindChild<Border>("WindowBorder");
            if (window.WindowState == WindowState.Maximized)
            {
                var source = PresentationSource.FromVisual(window);
                var device = source.CompositionTarget.TransformToDevice;
                var x = device.M11;
                var y = device.M22;

                var thickness = SystemParameters.WindowResizeBorderThickness;
                grid.Margin = new Thickness(4 / x, 4 / y, 4 / x, 4 / y);
                border.BorderThickness = new Thickness(thickness.Left / x, thickness.Top / y, thickness.Right / x, thickness.Bottom / y);
            }
            else
            {
                grid.Margin = new Thickness(0);
                border.BorderThickness = new Thickness(0);
            }
        }

        private bool IsTabletMode()
        {
            var value = Registry.GetValue(IsTabletModeKey, IsTabletModeValue, -1);
            return Equals(value, 1);
        }

        private void Toggle_WindowState(Window window)
        {
            var resizeMode = window.ResizeMode;
            if (resizeMode != ResizeMode.CanResize && resizeMode != ResizeMode.CanResizeWithGrip)
                return;
            var state = window.WindowState;
            if (state == WindowState.Maximized && this.IsTabletMode() == false)
                window.WindowState = WindowState.Normal;
            else if (state == WindowState.Normal)
                window.WindowState = WindowState.Maximized;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = this.FindAncestor<Window>();
            if (e.ClickCount == 2)
                this.Toggle_WindowState(window);
            else
                window.DragMove();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;
            var window = this.FindAncestor<Window>();
            switch (tag)
            {
                case "minimize":
                    window.WindowState = WindowState.Minimized;
                    break;

                case "toggle":
                    this.Toggle_WindowState(window);
                    break;

                case "exit":
                    window.Close();
                    break;
            }
        }
    }
}
