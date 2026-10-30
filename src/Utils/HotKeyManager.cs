using System;
using System.Windows.Interop;

namespace CrosshairMaster.Utils
{
    public class HotKeyManager : IDisposable
    {
        public const int HOTKEY_ID_OVERLAY = 9001;
        public const int HOTKEY_ID_SETTINGS = 9002;

        private IntPtr _hWnd;
        private HwndSource _source;

        public event Action OnToggleOverlay;
        public event Action OnToggleSettings;

        public HotKeyManager(IntPtr hWnd)
        {
            _hWnd = hWnd;
            _source = HwndSource.FromHwnd(hWnd);
            if (_source != null)
            {
                _source.AddHook(HwndHook);
            }
        }

        public void Register(int overlayVK, int overlayMod, int settingsVK, int settingsMod)
        {
            Unregister();
            if (overlayVK > 0)
            {
                Win32Helper.RegisterHotKey(_hWnd, HOTKEY_ID_OVERLAY, (uint)overlayMod, (uint)overlayVK);
            }
            if (settingsVK > 0)
            {
                Win32Helper.RegisterHotKey(_hWnd, HOTKEY_ID_SETTINGS, (uint)settingsMod, (uint)settingsVK);
            }
        }

        public void Unregister()
        {
            if (_hWnd != IntPtr.Zero)
            {
                Win32Helper.UnregisterHotKey(_hWnd, HOTKEY_ID_OVERLAY);
                Win32Helper.UnregisterHotKey(_hWnd, HOTKEY_ID_SETTINGS);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32Helper.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_OVERLAY)
                {
                    if (OnToggleOverlay != null)
                    {
                        OnToggleOverlay();
                    }
                    handled = true;
                }
                else if (id == HOTKEY_ID_SETTINGS)
                {
                    if (OnToggleSettings != null)
                    {
                        OnToggleSettings();
                    }
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }
    }
}
