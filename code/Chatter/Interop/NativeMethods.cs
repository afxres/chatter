using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Chatter.Interop
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hWnd, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        internal static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        public static int EnableAcrylicBlur(Window window)
        {
            const int AccentPolicy = 19;
            const int AccentEnableBlurBehind = 3;
            const int BlurOpacity = 0x00;
            const int BlurBackground = 0x000000;

            var result = default(int);
            var accentSize = Marshal.SizeOf<AccentPolicy>();
            var pointer = Marshal.AllocHGlobal(accentSize);
            if (pointer == IntPtr.Zero)
                throw new OutOfMemoryException();
            try
            {
                var windowHandle = new WindowInteropHelper(window).Handle;
                var accentPolicy = new AccentPolicy { AccentState = AccentEnableBlurBehind, GradientColor = (BlurOpacity << 24) | (BlurBackground & 0xFFFFFF) };
                Marshal.StructureToPtr(accentPolicy, pointer, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = AccentPolicy,
                    SizeOfData = accentSize,
                    Data = pointer,
                };
                result = SetWindowCompositionAttribute(windowHandle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
            return result;
        }

        public static int DisableSystemMenu(Window window)
        {
            const int GetWindowLongStyle = (-16);
            const int WindowStyleSystemMenu = 0x80000;
            var windowHandle = new WindowInteropHelper(window).Handle;
            var current = GetWindowLong(windowHandle, GetWindowLongStyle);
            var result = SetWindowLong(windowHandle, GetWindowLongStyle, current & ~WindowStyleSystemMenu);
            return result;
        }
    }
}
