using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CrosshairMaster.Models;
using CrosshairMaster.Utils;

namespace CrosshairMaster.Views
{
    public class OverlayWindow : Window
    {
        private Canvas _canvas;
        private Canvas _calibrationCanvas;
        private DispatcherTimer _topmostTimer;
        private List<CrosshairLayer> _layers;
        private bool _isOverlayActive = true;

        // Screen Resolution & Auto Centering State (⭐ USER REQUEST: 分辨率变动时自动刷新软件)
        public bool AutoRefreshEnabled { get; set; }
        public event Action<ScreenMetrics> OnResolutionChanged;
        private int _lastTrackedPhysicalW = 0;
        private int _lastTrackedPhysicalH = 0;
        private int _lastTrackedDpiPercent = 0;
        private bool _isHookRegistered = false;
        private DispatcherTimer _settleTimer1;
        private DispatcherTimer _settleTimer2;

        // Mouse Hold Hiding State (⭐ USER REQUEST: 只对勾选的单一图层隐藏)
        private DispatcherTimer _mousePollingTimer;
        private bool _lastLDown = false;
        private bool _lastRDown = false;

        // Screen Calibration / Dragging State (⭐ USER REQUEST: 屏幕直接拖拽定位)
        private bool _isCalibrationMode = false;
        private CrosshairLayer _calibrationLayer;
        private Action _onOffsetChangedCallback;
        private Action _onCalibrationFinishedCallback;

        private Border _calibrationAnchor;
        private Border _calibrationTopHud;
        private bool _isDraggingAnchor = false;
        private Point _anchorDragStartMouse;
        private double _anchorDragStartOffsetX;
        private double _anchorDragStartOffsetY;

        public OverlayWindow()
        {
            AutoRefreshEnabled = true;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Focusable = false;
            IsHitTestVisible = false;
            SnapsToDevicePixels = false;
            UseLayoutRounding = false;

            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics();
            Left = 0;
            Top = 0;
            Width = m.DipWidth;
            Height = m.DipHeight;

            Grid root = new Grid();

            _canvas = new Canvas
            {
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                SnapsToDevicePixels = false,
                UseLayoutRounding = false
            };
            root.Children.Add(_canvas);

            _calibrationCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                SnapsToDevicePixels = false,
                UseLayoutRounding = false
            };
            root.Children.Add(_calibrationCanvas);

            Content = root;

            _layers = new List<CrosshairLayer>();

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            KeyDown += OnOverlayKeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            Win32Helper.MakeClickThrough(hWnd);
            Win32Helper.ForceTopMost(hWnd);

            if (!_isHookRegistered && hWnd != IntPtr.Zero)
            {
                HwndSource source = HwndSource.FromHwnd(hWnd);
                if (source != null)
                {
                    source.AddHook(WndProc);
                    _isHookRegistered = true;
                }
            }

            try
            {
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            }
            catch { }

            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics();
            _lastTrackedPhysicalW = m.PhysicalWidth;
            _lastTrackedPhysicalH = m.PhysicalHeight;
            _lastTrackedDpiPercent = m.DpiPercent;

            Left = 0;
            Top = 0;
            Width = m.DipWidth;
            Height = m.DipHeight;

            if (hWnd != IntPtr.Zero)
            {
                Win32Helper.SetWindowPos(hWnd, Win32Helper.HWND_TOPMOST, 0, 0, m.PhysicalWidth, m.PhysicalHeight,
                    Win32Helper.SWP_NOACTIVATE | Win32Helper.SWP_SHOWWINDOW);
            }

            _topmostTimer = new DispatcherTimer();
            _topmostTimer.Interval = TimeSpan.FromSeconds(1.5);
            _topmostTimer.Tick += (s, ev) =>
            {
                if (Visibility == Visibility.Visible && !_isCalibrationMode)
                {
                    IntPtr handle = new WindowInteropHelper(this).Handle;
                    if (handle != IntPtr.Zero)
                    {
                        Win32Helper.ForceTopMost(handle);
                    }
                }
                CheckResolutionPolling();
            };
            _topmostTimer.Start();

            if (_isOverlayActive)
            {
                PlayActivationAnimation();
            }
        }

        // ======================= ⭐ Screen Resolution & Auto Centering =======================
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_DPICHANGED = 0x02E0;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DISPLAYCHANGE || msg == WM_DPICHANGED)
            {
                if (AutoRefreshEnabled)
                {
                    TriggerResolutionRefreshSequence();
                }
            }
            return IntPtr.Zero;
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (AutoRefreshEnabled)
            {
                TriggerResolutionRefreshSequence();
            }
        }

        private void CheckResolutionPolling()
        {
            if (!AutoRefreshEnabled) return;

            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics();
            if (_lastTrackedPhysicalW <= 0 || _lastTrackedPhysicalH <= 0)
            {
                _lastTrackedPhysicalW = m.PhysicalWidth;
                _lastTrackedPhysicalH = m.PhysicalHeight;
                _lastTrackedDpiPercent = m.DpiPercent;
                return;
            }

            if (m.PhysicalWidth != _lastTrackedPhysicalW || m.PhysicalHeight != _lastTrackedPhysicalH || m.DpiPercent != _lastTrackedDpiPercent)
            {
                TriggerResolutionRefreshSequence();
            }
        }

        public void TriggerResolutionRefreshSequence()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CheckAndRefreshResolution(true);
            }), DispatcherPriority.Render);

            if (_settleTimer1 == null)
            {
                _settleTimer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
                _settleTimer1.Tick += (s, e) =>
                {
                    _settleTimer1.Stop();
                    CheckAndRefreshResolution(true);
                };
            }
            _settleTimer1.Stop();
            _settleTimer1.Start();

            if (_settleTimer2 == null)
            {
                _settleTimer2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
                _settleTimer2.Tick += (s, e) =>
                {
                    _settleTimer2.Stop();
                    CheckAndRefreshResolution(true);
                };
            }
            _settleTimer2.Stop();
            _settleTimer2.Start();
        }

        public void CheckAndRefreshResolution(bool triggerEvent = true)
        {
            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics();
            _lastTrackedPhysicalW = m.PhysicalWidth;
            _lastTrackedPhysicalH = m.PhysicalHeight;
            _lastTrackedDpiPercent = m.DpiPercent;

            Left = 0;
            Top = 0;
            Width = m.DipWidth;
            Height = m.DipHeight;

            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            if (hWnd != IntPtr.Zero)
            {
                Win32Helper.SetWindowPos(hWnd, Win32Helper.HWND_TOPMOST, 0, 0, m.PhysicalWidth, m.PhysicalHeight,
                    Win32Helper.SWP_NOACTIVATE | Win32Helper.SWP_SHOWWINDOW);
            }

            UpdateLayout();
            Redraw();

            if (triggerEvent && OnResolutionChanged != null)
            {
                OnResolutionChanged(m);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        public void SetLayers(List<CrosshairLayer> layers)
        {
            _layers = layers != null ? new List<CrosshairLayer>(layers) : new List<CrosshairLayer>();
            UpdateMouseHideTimerState();
            Redraw();
        }

        public void SetOverlayActive(bool active)
        {
            _isOverlayActive = active;
            Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            if (active)
            {
                UpdateMouseHideTimerState();
                Redraw();
                PlayActivationAnimation();
            }
            else
            {
                if (_mousePollingTimer != null && _mousePollingTimer.IsEnabled)
                {
                    _mousePollingTimer.Stop();
                }
            }
        }

        public void UpdateMouseHideTimerState()
        {
            bool needsTimer = false;
            if (_layers != null)
            {
                foreach (CrosshairLayer l in _layers)
                {
                    if (l != null && l.Enabled && (l.HideOnLeftMouse || l.HideOnRightMouse))
                    {
                        needsTimer = true;
                        break;
                    }
                }
            }

            if (needsTimer)
            {
                if (_mousePollingTimer == null)
                {
                    _mousePollingTimer = new DispatcherTimer(DispatcherPriority.Render);
                    _mousePollingTimer.Interval = TimeSpan.FromMilliseconds(8); // ~120Hz check
                    _mousePollingTimer.Tick += OnMousePollingTick;
                }
                if (!_mousePollingTimer.IsEnabled)
                {
                    _mousePollingTimer.Start();
                }
            }
            else
            {
                if (_mousePollingTimer != null && _mousePollingTimer.IsEnabled)
                {
                    _mousePollingTimer.Stop();
                }
                _lastLDown = false;
                _lastRDown = false;
                Redraw();
            }
        }

        private void OnMousePollingTick(object sender, EventArgs e)
        {
            if (!_isOverlayActive || _isCalibrationMode) return;

            bool isLDown = (Win32Helper.GetAsyncKeyState(Win32Helper.VK_LBUTTON) & 0x8000) != 0;
            bool isRDown = (Win32Helper.GetAsyncKeyState(Win32Helper.VK_RBUTTON) & 0x8000) != 0;

            if (isLDown != _lastLDown || isRDown != _lastRDown)
            {
                _lastLDown = isLDown;
                _lastRDown = isRDown;
                Redraw();
            }
        }

        // ⭐ 开启准星时的战术锁定动画 (Tactical Holographic Acquire Animation)
        public void PlayActivationAnimation()
        {
            if (_canvas == null) return;
            try
            {
                ScaleTransform st = new ScaleTransform(1.32, 1.32);
                _canvas.RenderTransformOrigin = new Point(0.5, 0.5);
                _canvas.RenderTransform = st;

                DoubleAnimation scaleAnim = new DoubleAnimation(1.32, 1.0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
                };
                DoubleAnimation opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                _canvas.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            }
            catch { }
        }

        public void Redraw()
        {
            if (!_isOverlayActive || _canvas == null) return;

            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics();
            double cx = m.DipCenterX;
            double cy = m.DipCenterY;

            bool isLDown = (Win32Helper.GetAsyncKeyState(Win32Helper.VK_LBUTTON) & 0x8000) != 0;
            bool isRDown = (Win32Helper.GetAsyncKeyState(Win32Helper.VK_RBUTTON) & 0x8000) != 0;

            // ⭐ USER REQUEST: 只对勾选的单一图层隐藏 (未勾选图层始终正常渲染显示)
            List<CrosshairLayer> activeLayers = new List<CrosshairLayer>();
            if (_layers != null)
            {
                foreach (CrosshairLayer l in _layers)
                {
                    if (l == null || !l.Enabled) continue;
                    if (l.HideOnLeftMouse && isLDown) continue;
                    if (l.HideOnRightMouse && isRDown) continue;
                    activeLayers.Add(l);
                }
            }

            CrosshairRenderer.RenderLayers(_canvas, activeLayers, cx, cy, 1.0);

            // If in calibration mode, position calibration handle and HUD
            if (_isCalibrationMode && _calibrationLayer != null)
            {
                UpdateCalibrationUI(cx, cy);
            }
        }

        // ======================= ⭐ Screen Dragging Calibration Mode =======================
        public void StartScreenCalibration(CrosshairLayer targetLayer, Action onOffsetChanged, Action onFinished)
        {
            _isCalibrationMode = true;
            _calibrationLayer = targetLayer;
            _onOffsetChangedCallback = onOffsetChanged;
            _onCalibrationFinishedCallback = onFinished;

            // Remove WS_EX_TRANSPARENT so mouse clicks can interact across the screen
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            IntPtr curExStyle = Win32Helper.GetWindowLongPtr(hWnd, Win32Helper.GWL_EXSTYLE);
            long newExStyle = curExStyle.ToInt64() & ~Win32Helper.WS_EX_TRANSPARENT;
            Win32Helper.SetWindowLongPtr(hWnd, Win32Helper.GWL_EXSTYLE, new IntPtr(newExStyle));

            this.IsHitTestVisible = true;
            this.Focusable = true;
            this.Focus();

            _calibrationCanvas.IsHitTestVisible = true;
            _calibrationCanvas.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // catches click anywhere on screen
            _calibrationCanvas.Cursor = Cursors.SizeAll;

            BuildCalibrationElements();
            Redraw();
        }

        public void StopScreenCalibration()
        {
            if (!_isCalibrationMode) return;
            _isCalibrationMode = false;

            if (_isDraggingAnchor)
            {
                _isDraggingAnchor = false;
                _calibrationCanvas.ReleaseMouseCapture();
            }

            // Clean up UI in calibration layer
            _calibrationCanvas.Children.Clear();
            _calibrationAnchor = null;
            _calibrationTopHud = null;
            _calibrationCanvas.IsHitTestVisible = false;
            _calibrationCanvas.Background = Brushes.Transparent;
            _calibrationCanvas.Cursor = Cursors.Arrow;

            // Restore WS_EX_TRANSPARENT
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            Win32Helper.MakeClickThrough(hWnd);

            this.IsHitTestVisible = false;
            this.Focusable = false;

            Redraw();

            if (_onCalibrationFinishedCallback != null)
            {
                _onCalibrationFinishedCallback();
            }
        }

        private void BuildCalibrationElements()
        {
            _calibrationCanvas.Children.Clear();

            // 1. Draggable Anchor Ring at Crosshair Position
            _calibrationAnchor = new Border
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Color.FromArgb(45, 0, 229, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                BorderThickness = new Thickness(2.5),
                CornerRadius = new CornerRadius(50),
                Cursor = Cursors.SizeAll,
                ToolTip = "在屏幕任意位置或按住此圆圈拖拽，准星将随鼠标实时移动，按ESC或点击右上角锁定退出"
            };

            StackPanel anchorStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock txtIcon = new TextBlock
            {
                Text = "✛ 屏幕拖拽中",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            anchorStack.Children.Add(txtIcon);
            _calibrationAnchor.Child = anchorStack;

            // Handle dragging anywhere on the screen (calibration canvas)
            _calibrationCanvas.MouseDown += OnCalibrationMouseDown;
            _calibrationCanvas.MouseMove += OnCalibrationMouseMove;
            _calibrationCanvas.MouseUp += OnCalibrationMouseUp;

            _calibrationCanvas.Children.Add(_calibrationAnchor);

            // 2. Top-Center Floating HUD Banner
            _calibrationTopHud = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 18, 20, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 10, 18, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = Cursors.Arrow
            };
            // Prevent HUD click from triggering anchor drag
            _calibrationTopHud.MouseDown += (s, e) => { e.Handled = true; };

            StackPanel hudStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock hudTip = new TextBlock
            {
                Text = "🎯【物理屏幕准星直接标定】在屏幕任意位置按住鼠标左键即可拖拽准星到目标点 | 调整好后点击右侧 ➔",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Button btnFinish = new Button
            {
                Content = "🔒 完成并锁定 (ESC)",
                Height = 32,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 180, 100)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnFinish.Click += (s, e) => StopScreenCalibration();

            hudStack.Children.Add(hudTip);
            hudStack.Children.Add(btnFinish);
            _calibrationTopHud.Child = hudStack;

            _calibrationCanvas.Children.Add(_calibrationTopHud);
        }

        private void OnCalibrationMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _calibrationLayer != null)
            {
                _isDraggingAnchor = true;
                _anchorDragStartMouse = e.GetPosition(this);
                _anchorDragStartOffsetX = _calibrationLayer.OffsetX;
                _anchorDragStartOffsetY = _calibrationLayer.OffsetY;
                _calibrationCanvas.CaptureMouse();
            }
        }

        private void OnCalibrationMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingAnchor && _calibrationLayer != null)
            {
                Point curMouse = e.GetPosition(this);
                double dx = curMouse.X - _anchorDragStartMouse.X;
                double dy = curMouse.Y - _anchorDragStartMouse.Y;

                _calibrationLayer.OffsetX = Math.Round(_anchorDragStartOffsetX + dx, 3);
                _calibrationLayer.OffsetY = Math.Round(_anchorDragStartOffsetY + dy, 3);

                if (_onOffsetChangedCallback != null)
                {
                    _onOffsetChangedCallback();
                }
                Redraw();
            }
        }

        private void OnCalibrationMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingAnchor)
            {
                _isDraggingAnchor = false;
                _calibrationCanvas.ReleaseMouseCapture();
            }
        }

        private void UpdateCalibrationUI(double cx, double cy)
        {
            if (_calibrationAnchor != null)
            {
                double targetX = cx + _calibrationLayer.OffsetX - 50;
                double targetY = cy + _calibrationLayer.OffsetY - 50;
                Canvas.SetLeft(_calibrationAnchor, targetX);
                Canvas.SetTop(_calibrationAnchor, targetY);
            }

            if (_calibrationTopHud != null)
            {
                Canvas.SetLeft(_calibrationTopHud, (ActualWidth - 780) / 2.0);
                Canvas.SetTop(_calibrationTopHud, 35);
            }
        }

        private void OnOverlayKeyDown(object sender, KeyEventArgs e)
        {
            if (_isCalibrationMode)
            {
                if (e.Key == Key.Escape || e.Key == Key.Enter)
                {
                    StopScreenCalibration();
                    e.Handled = true;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            }
            catch { }
            if (_settleTimer1 != null)
            {
                _settleTimer1.Stop();
                _settleTimer1 = null;
            }
            if (_settleTimer2 != null)
            {
                _settleTimer2.Stop();
                _settleTimer2 = null;
            }
            if (_topmostTimer != null)
            {
                _topmostTimer.Stop();
                _topmostTimer = null;
            }
            if (_mousePollingTimer != null)
            {
                _mousePollingTimer.Stop();
                _mousePollingTimer = null;
            }
            base.OnClosed(e);
        }
    }
}
