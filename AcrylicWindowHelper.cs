using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.ComponentModel;

namespace GlobalMediaControl
{
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    public class AcrylicWindowHelper
    {

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto)]
        public static extern int GetCurrentThemeName(StringBuilder pszThemeFileName, int dwMaxNameChars, StringBuilder pszColorBuff, int dwMaxColorChars, StringBuilder pszSizeBuff, int cchMaxSizeChars);

        public static Popup GetPopupFromVisualChild(Visual child)
        {
            Visual parent = child;
            FrameworkElement visualRoot = null;

            //Traverse the visual tree up to find the PopupRoot instance.
            while (parent != null)
            {
                visualRoot = parent as FrameworkElement;
                parent = VisualTreeHelper.GetParent(parent) as Visual;
            }

            Popup popup = null;

            // Examine the PopupRoot's logical parent to get the Popup instance.
            // This might break in the future since it relies on the internal implementation of Popup's element tree.
            if (visualRoot != null)
            {
                popup = visualRoot.Parent as Popup;
            }

            return popup;
        }

        public static void EnableBlur(Visual visual)
        {
            // BGR color
            // var settings = new Windows.UI.ViewManagement.UISettings();
            // var bgColor = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            // var color = System.Drawing.Color.FromArgb(bgColor.A, bgColor.R, bgColor.G, bgColor.B);
            // var color = Properties.Settings.Default.AcrylicColor;
            var color = Properties.Settings.Default.AcrylicColor;
            var color1 = System.Drawing.ColorTranslator.FromHtml(color);
            var intColor = System.Drawing.ColorTranslator.ToWin32(color1);
            EnableBlur(visual, Properties.Settings.Default.AcrylicOpacity, (uint)intColor);
        }

        public static void EnableBlur(Visual visual, uint opacity, uint color)
        {
            //var hs= new WindowInteropHelper(win);
            HwndSource hs = (HwndSource)HwndSource.FromVisual(visual);

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = (opacity << 24) | (color & 0xFFFFFF);

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(hs.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public static void SwitchTheme()
        {
            var settings = new Windows.UI.ViewManagement.UISettings();
            var bgColor = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            var fgColor = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            var colorBg = System.Drawing.Color.FromArgb(bgColor.A, bgColor.R, bgColor.G, bgColor.B);
            var colorFg = System.Drawing.Color.FromArgb(fgColor.A, fgColor.R, fgColor.G, fgColor.B);
            Properties.Settings.Default.AcrylicColor = System.Drawing.ColorTranslator.ToHtml(colorBg);
            Properties.Settings.Default.ForegroundColor = System.Drawing.ColorTranslator.ToHtml(colorFg);
        }
    }
}
