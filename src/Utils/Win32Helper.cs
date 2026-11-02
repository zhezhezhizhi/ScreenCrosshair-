using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace CrosshairMaster.Utils
{
    public static class Win32Helper
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x00000008;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const int WM_HOTKEY = 0x0312;

        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        public const int HORZRES = 8;
        public const int VERTRES = 10;
        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;
        public const int DESKTOPVERTRES = 117;
        public const int DESKTOPHORZRES = 118;

        [DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        public static void MakeClickThrough(IntPtr hWnd)
        {
            IntPtr curExStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
            long newExStyle = curExStyle.ToInt64() 
                | WS_EX_TRANSPARENT 
                | WS_EX_LAYERED 
                | WS_EX_TOOLWINDOW 
                | WS_EX_NOACTIVATE;
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(newExStyle));
        }

        public static void ForceTopMost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        public static ScreenMetrics GetPrimaryScreenMetrics()
        {
            ScreenMetrics m = new ScreenMetrics();
            int physW = 0;
            int physH = 0;

            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    physW = GetDeviceCaps(hdc, DESKTOPHORZRES);
                    physH = GetDeviceCaps(hdc, DESKTOPVERTRES);
                    ReleaseDC(IntPtr.Zero, hdc);
                }
            }
            catch { }

            if (physW <= 0 || physH <= 0)
            {
                try
                {
                    physW = GetSystemMetrics(SM_CXSCREEN);
                    physH = GetSystemMetrics(SM_CYSCREEN);
                }
                catch { }
            }

            if (physW <= 0) physW = 1920;
            if (physH <= 0) physH = 1080;

            m.PhysicalWidth = physW;
            m.PhysicalHeight = physH;

            // In WPF, SystemParameters.PrimaryScreenWidth / PrimaryScreenHeight gives logical DIP bounds.
            double dipW = 0;
            double dipH = 0;
            try
            {
                dipW = SystemParameters.PrimaryScreenWidth;
                dipH = SystemParameters.PrimaryScreenHeight;
            }
            catch { }

            if (dipW <= 0 || dipH <= 0)
            {
                dipW = physW;
                dipH = physH;
            }

            m.DipWidth = dipW;
            m.DipHeight = dipH;

            // Calculate dynamic scale between physical pixels and WPF DIP coordinates
            double scale = (dipW > 0 && physW > 0) ? ((double)physW / dipW) : 1.0;
            int percent = (int)Math.Round(scale * 100.0);

            // Handle slight rounding variations around 100% (e.g. 99% or 101%)
            if (Math.Abs(percent - 100) <= 2)
            {
                percent = 100;
                scale = 1.0;
                m.DipWidth = physW;
                m.DipHeight = physH;
            }

            m.DpiScale = scale;
            m.DpiPercent = percent;

            return m;
        }
    }

    public class ScreenMetrics
    {
        public int PhysicalWidth { get; set; }
        public int PhysicalHeight { get; set; }
        public double DpiScale { get; set; }
        public int DpiPercent { get; set; }
        public double DipWidth { get; set; }
        public double DipHeight { get; set; }

        public int PhysicalCenterX { get { return PhysicalWidth / 2; } }
        public int PhysicalCenterY { get { return PhysicalHeight / 2; } }
        public double DipCenterX { get { return DipWidth / 2.0; } }
        public double DipCenterY { get { return DipHeight / 2.0; } }

        public override string ToString()
        {
            if (DpiPercent != 100)
            {
                return string.Format("{0}×{1} (物理) · 缩放 {2}% (逻辑 {3}×{4})", 
                    PhysicalWidth, PhysicalHeight, DpiPercent, (int)DipWidth, (int)DipHeight);
            }
            return string.Format("{0}×{1}", PhysicalWidth, PhysicalHeight);
        }
    }
}
