using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using CrosshairMaster.Models;
using CrosshairMaster.Utils;
using CrosshairMaster.Views;
using Application = System.Windows.Application;

namespace CrosshairMaster
{
    public static class Program
    {
        private static Mutex _singleInstanceMutex;
        private static NotifyIcon _trayIcon;
        private static HotKeyManager _hotKeyManager;

        [STAThread]
        public static void Main()
        {
            // 1. High DPI Awareness
            try
            {
                Win32Helper.SetProcessDPIAware();
            }
            catch { }

            // 2. Single instance check
            bool isNewInstance;
            _singleInstanceMutex = new Mutex(true, "ScreenCrosshair_v72_SingleInstance_Mutex", out isNewInstance);
            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("ScreenCrosshair 已在运行中。\n快捷键：F10 呼出面板，F8 开关准星。", "ScreenCrosshair v7.2", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 3. Create WPF Application
            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 4. Load configuration
            AppConfig config = AppConfig.Load();

            // 5. Create Overlay Window
            OverlayWindow overlayWindow = new OverlayWindow();
            overlayWindow.TargetMonitorIndex = config.TargetMonitorIndex;
            overlayWindow.AutoRefreshEnabled = config.AutoRefreshOnResolutionChange;
            overlayWindow.SetLayers(config.Layers);
            overlayWindow.SetOverlayActive(config.OverlayEnabled);
            overlayWindow.Show();

            // 6. Create Main Settings Window
            MainWindow mainWindow = new MainWindow(config, overlayWindow);

            // 7. Setup System Tray Icon
            SetupTrayIcon(app, config, overlayWindow, mainWindow);

            // 8. Setup Global Hotkeys using MainWindow's HWND
            try
            {
                IntPtr mainHwnd = new WindowInteropHelper(mainWindow).EnsureHandle();
                _hotKeyManager = new HotKeyManager(mainHwnd);
                mainWindow.SetHotKeyManager(_hotKeyManager);
                _hotKeyManager.OnToggleOverlay += () =>
                {
                    mainWindow.ToggleOverlay();
                };
                _hotKeyManager.OnToggleSettings += () =>
                {
                    if (mainWindow.IsVisible)
                    {
                        mainWindow.Hide();
                    }
                    else
                    {
                        mainWindow.ShowWindowAnimated();
                    }
                };
                _hotKeyManager.Register(config.ToggleHotKeyVK, config.ToggleHotKeyMod, config.SettingsHotKeyVK, config.SettingsHotKeyMod);
            }
            catch { }

            // 9. Launch Minimalist Startup Animation (⭐ USER REQUEST: 极简开启动画)
            StartupWindow startup = new StartupWindow();
            startup.OnAnimationCompleted += () =>
            {
                mainWindow.ShowWindowAnimated();
            };
            startup.Show();

            // 10. Handle Exit
            app.Exit += (s, e) =>
            {
                config.Save();
                if (_hotKeyManager != null)
                {
                    _hotKeyManager.Dispose();
                }
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
            };

            app.Run();
        }

        private static void SetupTrayIcon(Application app, AppConfig config, OverlayWindow overlay, MainWindow mainWin)
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "ScreenCrosshair (按 F10 呼出)";

            // Extract icon from embedded application icon or fallback
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using (Icon sysIcon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (sysIcon != null)
                    {
                        _trayIcon.Icon = (Icon)sysIcon.Clone();
                    }
                }
            }
            catch { }

            if (_trayIcon.Icon == null)
            {
                // Fallback dynamically generated in memory
                using (Bitmap bmp = new Bitmap(32, 32))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(System.Drawing.Color.Transparent);
                        using (System.Drawing.Pen greenPen = new System.Drawing.Pen(System.Drawing.Color.Lime, 3))
                        {
                            g.DrawLine(greenPen, 2, 16, 30, 16);
                            g.DrawLine(greenPen, 16, 2, 16, 30);
                        }
                        using (System.Drawing.SolidBrush redDot = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
                        {
                            g.FillEllipse(redDot, 13, 13, 6, 6);
                        }
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    try
                    {
                        using (Icon icon = Icon.FromHandle(hIcon))
                        {
                            _trayIcon.Icon = (Icon)icon.Clone();
                        }
                    }
                    finally
                    {
                        Win32Helper.DestroyIcon(hIcon);
                    }
                }
            }
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => mainWin.ShowWindowAnimated();

            ContextMenu menu = new ContextMenu();

            MenuItem miShowSettings = new MenuItem("显示面板 (F10)", (s, e) =>
            {
                mainWin.ShowWindowAnimated();
            });

            MenuItem miToggle = new MenuItem("开启/隐藏准星 (F8)", (s, e) =>
            {
                mainWin.ToggleOverlay();
            });

            MenuItem miRefreshRes = new MenuItem("刷新分辨率", (s, e) =>
            {
                mainWin.CheckAndRefreshResolution(true);
            });

            MenuItem miSep = new MenuItem("-");

            MenuItem miExit = new MenuItem("退出", (s, e) =>
            {
                config.Save();
                app.Shutdown();
            });

            menu.MenuItems.Add(miShowSettings);
            menu.MenuItems.Add(miToggle);
            menu.MenuItems.Add(miRefreshRes);
            menu.MenuItems.Add(miSep);
            menu.MenuItems.Add(miExit);

            _trayIcon.ContextMenu = menu;
        }
    }
}
