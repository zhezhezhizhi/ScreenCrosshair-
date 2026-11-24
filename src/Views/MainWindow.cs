using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using CrosshairMaster.Models;
using CrosshairMaster.Utils;

namespace CrosshairMaster.Views
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;
        private OverlayWindow _overlayWindow;
        private HotKeyManager _hotKeyManager;
        private CrosshairLayer _selectedLayer;
        private CrosshairGroup _selectedGroup;

        private Grid _hostGrid;

        // Custom Window Wallpaper (⭐ USER REQUEST: 软件界面背景自定义)
        private Image _windowBgImage;
        private Border _windowDarkOverlay;
        private TextBlock _txtWindowWallpaperPath;
        private Slider _sliderWindowWallpaperOpacity;
        private TextBox _txtWindowWallpaperOpacityVal;

        // UI Tabs & Containers
        private Grid _mainContentGrid;
        private Grid _designerTabGrid;
        private ScrollViewer _settingsTabScroll;
        private Button _tabBtnDesigner;
        private Button _tabBtnSettings;

        // Dynamic Themed Elements Trackers
        private List<Border> _cardBorders = new List<Border>();
        private List<Control> _themedInputs = new List<Control>();
        private List<TextBlock> _sectionHeaders = new List<TextBlock>();
        private Border _topBorder;
        private Border _bottomBorder;

        // Unified Apple UI Palette Constants
        public static readonly Color NeutralBtnColor = Color.FromRgb(44, 44, 48);
        public static readonly Color DangerBtnColor = Color.FromRgb(58, 30, 32);

        // Theme Brushes
        private SolidColorBrush _themeAccentBrush;
        private SolidColorBrush _themeBgBrush;
        private SolidColorBrush _themeCardBrush;
        private SolidColorBrush _themeHeaderBrush;
        private SolidColorBrush _themeBorderBrush;
        private SolidColorBrush _themeInputBgBrush;
        private SolidColorBrush _themeInputTextBrush;
        private SolidColorBrush _themeButtonBrush;
        private SolidColorBrush _themeDangerBrush;
        private SolidColorBrush _themeTextBrush;
        private SolidColorBrush _themeSubTextBrush;

        private class UiColorEditorItem
        {
            public string Title;
            public Border Swatch;
            public TextBox TxtHex;
            public Func<string> Getter;
            public Action<string> Setter;
        }

        private List<UiColorEditorItem> _uiColorEditors = new List<UiColorEditorItem>();
        private static ControlTemplate _comboBoxTemplateCache;

        // Header Controls
        private TextBlock _logoText;
        private TextBlock _statusBadge;
        private Button _btnToggleOverlay;

        // Preview Controls
        private Border _previewBox;
        private Canvas _previewCanvas;
        private Button _btnPreviewSingle;
        private Button _btnPreviewAll;
        private ComboBox _cboPreviewBg;
        private CheckBox _chkPixelGrid;
        private TextBlock _txtZoomVal;
        private TextBox _txtOffsetX;
        private TextBox _txtOffsetY;
        private TextBlock _txtHkOverlayHint;
        private TextBlock _txtHkSettingsHint;
        private StackPanel _zoomPillsPanel;

        // Screen Resolution & Auto Centering Controls (⭐ USER REQUEST: 屏幕分辨率检测与自动居中刷新)
        private TextBlock _lblScreenResInfo;
        private TextBlock _lblSettingsResInfo;
        private CheckBox _chkAutoRefreshRes;
        private TextBlock _txtBottomStatus;

        // Position Dragging & Locking Controls
        private Button _btnLockPosition;
        private TextBlock _txtLockStatus;
        private bool _isDraggingInPreview = false;
        private Point _dragStartPoint;
        private double _dragStartOffsetX;
        private double _dragStartOffsetY;

        // Layout Columns & Splitters (⭐ USER REQUEST: 三个大界面支持拖拽调比例)
        private ColumnDefinition _colLeft;
        private ColumnDefinition _colCenter;
        private ColumnDefinition _colRight;
        private GridSplitter _splitterLeft;
        private GridSplitter _splitterRight;

        // Layer List Controls
        private StackPanel _layerListPanel;

        // Group Management Controls
        private ComboBox _cboGroups;
        private StackPanel _groupLayersPanel;

        // Inspector Controls
        private TextBox _txtName;
        private ComboBox _cboMode;
        private Slider _sliderLength;
        private TextBox _txtLengthVal;
        private Slider _sliderThickness;
        private TextBox _txtThicknessVal;
        private Slider _sliderGap;
        private TextBox _txtGapVal;
        private Slider _sliderRotation;
        private TextBox _txtRotationVal;
        private Slider _sliderAlpha;
        private TextBox _txtAlphaVal;
        private TextBox _txtColorHex;

        private CheckBox _chkOutline;
        private Slider _sliderOutline;
        private TextBox _txtOutlineVal;

        // Center Dot Controls (Square / Circle)
        private CheckBox _chkDot;
        private ComboBox _cboDotShape;
        private Slider _sliderDotSize;
        private TextBox _txtDotSizeVal;
        private TextBox _txtDotColor;
        private CheckBox _chkDotOutline;

        // Center Circle Controls
        private CheckBox _chkCircle;
        private Slider _sliderCircleRadius;
        private TextBox _txtCircleRadiusVal;
        private Slider _sliderCircleThick;
        private TextBox _txtCircleThickVal;
        private TextBox _txtCircleColor;

        // Custom Image Reticle Controls (⭐ USER REQUEST: 自定义图片准星 & 纵横比锁定)
        private Border _customImageBox;
        private TextBlock _txtCustomImagePath;
        private CheckBox _chkLockAspectRatio;
        private Slider _sliderImageW;
        private TextBox _txtImageWVal;
        private Slider _sliderImageH;
        private TextBox _txtImageHVal;
        private Slider _sliderImageRot;
        private TextBox _txtImageRotVal;

        // Mouse Hold Hiding Controls (⭐ USER REQUEST: 准星调整里针对勾选的单一图层隐藏)
        private CheckBox _chkLayerHideOnLeftMouse;
        private CheckBox _chkLayerHideOnRightMouse;

        // Hotkey Recording state
        private bool _isRecordingOverlayKey = false;
        private bool _isRecordingSettingsKey = false;
        private Button _btnRecordOverlayKey;
        private Button _btnRecordSettingsKey;

        // Theme / Customization Controls in Settings Tab
        private ComboBox _cboThemePresets;
        private CheckBox _chkStartWithWindows;

        private bool _isUpdatingUi = false;

        public MainWindow(AppConfig config, OverlayWindow overlay, HotKeyManager hotkeys = null)
        {
            _config = config;
            _overlayWindow = overlay;
            _hotKeyManager = hotkeys;

            Title = "ScreenCrosshair v7.2";
            Width = 1200;
            Height = 800;
            MinWidth = 1060;
            MinHeight = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            ComputeThemeBrushes();

            if (_config.Layers.Count > 0)
            {
                _selectedLayer = _config.Layers.Find(l => l.Id == _config.SelectedLayerId);
                if (_selectedLayer == null)
                {
                    _selectedLayer = _config.Layers[0];
                }
            }

            if (_config.Groups != null && _config.Groups.Count > 0)
            {
                _selectedGroup = _config.Groups.Find(g => g.Id == _config.SelectedGroupId);
                if (_selectedGroup == null)
                {
                    _selectedGroup = _config.Groups[0];
                }
            }

            PreviewKeyDown += OnWindowPreviewKeyDown;

            // Ensure crisp, sharp font rendering across high DPI displays
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

            LoadAppIcon();
            Loaded += (s, e) => PlayStartupAnimation();
            Closing += (s, e) => { SaveColumnRatios(); };

            BuildUI();
            ApplyThemeToAllControls();
            RefreshGroupList();
            RefreshLayerList();
            LoadSelectedLayerIntoInspector();
            UpdateWindowBackground();
            UpdatePreviewBackground();
            UpdateAllViews();
            UpdateResolutionUI();

            // ⭐ USER REQUEST: 屏幕分辨率变动时自动刷新软件与准星居中
            if (_overlayWindow != null)
            {
                _overlayWindow.OnResolutionChanged += (metrics) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateResolutionUI(metrics);
                        UpdatePreview();
                    }), DispatcherPriority.Render);
                };
            }
        }

        // ======================= ⭐ Screen Resolution & Centering Methods =======================
        public void UpdateResolutionUI(ScreenMetrics m = null)
        {
            if (m == null) m = Win32Helper.GetPrimaryScreenMetrics();

            string resDisplay;
            if (m.DpiPercent != 100)
            {
                resDisplay = string.Format("🖥️ 屏幕: {0}×{1} (物理) · {2}% 缩放 (中心点: {3}, {4})",
                    m.PhysicalWidth, m.PhysicalHeight, m.DpiPercent, m.PhysicalCenterX, m.PhysicalCenterY);
            }
            else
            {
                resDisplay = string.Format("🖥️ 屏幕: {0}×{1} (0,0 居中点: {2}, {3})",
                    m.PhysicalWidth, m.PhysicalHeight, m.PhysicalCenterX, m.PhysicalCenterY);
            }

            if (_lblScreenResInfo != null)
            {
                _lblScreenResInfo.Text = resDisplay;
            }
            if (_lblSettingsResInfo != null)
            {
                if (m.DpiPercent != 100)
                {
                    _lblSettingsResInfo.Text = string.Format(
                        "物理屏幕分辨率: {0} × {1}  |  Windows 系统缩放: {2}%\n绝对物理中心点: X = {3} px, Y = {4} px  (工作区视口: {5} × {6})",
                        m.PhysicalWidth, m.PhysicalHeight, m.DpiPercent, m.PhysicalCenterX, m.PhysicalCenterY, (int)m.DipWidth, (int)m.DipHeight);
                }
                else
                {
                    _lblSettingsResInfo.Text = string.Format(
                        "物理屏幕分辨率: {0} × {1}  |  绝对物理中心点: X = {2} px, Y = {3} px",
                        m.PhysicalWidth, m.PhysicalHeight, m.PhysicalCenterX, m.PhysicalCenterY);
                }
            }
            if (_txtBottomStatus != null)
            {
                if (m.DpiPercent != 100)
                {
                    _txtBottomStatus.Text = string.Format(
                        "ScreenCrosshair v7.2 · 全局准星引擎已就绪 · 分辨率: {0}×{1} (缩放 {2}%) · by zhezhi",
                        m.PhysicalWidth, m.PhysicalHeight, m.DpiPercent);
                }
                else
                {
                    _txtBottomStatus.Text = string.Format(
                        "ScreenCrosshair v7.2 · 全局准星引擎已就绪 · 分辨率: {0}×{1} · by zhezhi",
                        m.PhysicalWidth, m.PhysicalHeight);
                }
            }

            // Keep MainWindow within visible bounds if resolution shrank
            if (Left + Width > m.DipWidth || Top + Height > m.DipHeight)
            {
                if (Left + Width > m.DipWidth) Left = Math.Max(0, (m.DipWidth - Width) / 2);
                if (Top + Height > m.DipHeight) Top = Math.Max(0, (m.DipHeight - Height) / 2);
            }
        }

        public void CheckAndRefreshResolution(bool isManual = false)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.CheckAndRefreshResolution(true);
            }
            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics(_config != null ? _config.TargetMonitorIndex : 0);
            UpdateResolutionUI(m);
            UpdatePreview();

            if (isManual)
            {
                string msg;
                if (m.DpiPercent != 100)
                {
                    msg = string.Format(
                        "屏幕物理分辨率检测与准星居中刷新成功！\n\n" +
                        "【真实物理分辨率】：{0} × {1}\n" +
                        "【Windows 显示缩放】：{2}%\n" +
                        "【屏幕物理正中心】：X = {3} px, Y = {4} px\n" +
                        "【应用工作区视口】：{5} × {6}\n\n" +
                        "✅ 全局准星视口已 100% 对齐并锁定至物理屏幕正中心！",
                        m.PhysicalWidth, m.PhysicalHeight, m.DpiPercent, m.PhysicalCenterX, m.PhysicalCenterY, (int)m.DipWidth, (int)m.DipHeight);
                }
                else
                {
                    msg = string.Format(
                        "屏幕物理分辨率检测与准星居中刷新成功！\n\n" +
                        "【当前屏幕物理分辨率】：{0} × {1}\n" +
                        "【屏幕绝对正中心】：X = {2} px, Y = {3} px\n\n" +
                        "✅ 全局准星视口已 100% 对齐并锁定至物理屏幕正中心！",
                        m.PhysicalWidth, m.PhysicalHeight, m.PhysicalCenterX, m.PhysicalCenterY);
                }

                MessageBox.Show(msg, "分辨率检测成功 - ScreenCrosshair v7.2", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void SetHotKeyManager(HotKeyManager hotkeys)
        {
            _hotKeyManager = hotkeys;
        }

        private void LoadAppIcon()
        {
            try
            {
                string icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app.ico");
                if (File.Exists(icoPath))
                {
                    Icon = BitmapFrame.Create(new Uri(icoPath, UriKind.Absolute));
                    return;
                }

                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using (System.Drawing.Icon sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                {
                    if (sysIcon != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            sysIcon.Save(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            Icon = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        }
                    }
                }
            }
            catch { }
        }

        // ⭐ 开启软件窗口时的平滑过渡动画 (Smooth Launch / Entrance Transition)
        public void ShowWindowAnimated()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            PlayStartupAnimation();
        }

        public void PlayStartupAnimation()
        {
            if (_hostGrid == null) return;
            try
            {
                ScaleTransform st = new ScaleTransform(0.94, 0.94);
                _hostGrid.RenderTransformOrigin = new Point(0.5, 0.5);
                _hostGrid.RenderTransform = st;

                DoubleAnimation scaleAnim = new DoubleAnimation(0.94, 1.0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                DoubleAnimation opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(240))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                _hostGrid.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

                if (_logoText != null)
                {
                    DoubleAnimation glowAnim = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(450))
                    {
                        EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
                    };
                    _logoText.BeginAnimation(UIElement.OpacityProperty, glowAnim);
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            // Outer host grid: Layer 0 wallpaper image, Layer 1 dark tint, Layer 2 main rootGrid
            _hostGrid = new Grid();

            _windowBgImage = new Image { Stretch = Stretch.UniformToFill };
            _windowDarkOverlay = new Border { Background = new SolidColorBrush(Color.FromArgb(175, 14, 14, 22)) };
            _hostGrid.Children.Add(_windowBgImage);
            _hostGrid.Children.Add(_windowDarkOverlay);

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });

            // 1. Top Bar
            _topBorder = new Border
            {
                Background = _themeHeaderBrush,
                BorderBrush = _themeBorderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(15, 10, 15, 10)
            };
            Grid topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // 简约现代准星图标徽标 (Minimalist Header Icon)
            Image imgLogo = new Image
            {
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true
            };
            string logoIcoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app_icon.png");
            if (File.Exists(logoIcoPath))
            {
                try
                {
                    imgLogo.Source = new BitmapImage(new Uri(logoIcoPath, UriKind.Absolute));
                }
                catch { }
            }
            titlePanel.Children.Add(imgLogo);

            _logoText = new TextBlock
            {
                Text = "SCREEN CROSSHAIR",
                FontWeight = FontWeights.Bold,
                FontSize = 17,
                Foreground = _themeAccentBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(_logoText);

            Border badgeVer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 10, 132, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 10, 132, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock txtVer = new TextBlock
            {
                Text = "v7.2",
                Foreground = _themeAccentBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            badgeVer.Child = txtVer;
            titlePanel.Children.Add(badgeVer);
            Grid.SetColumn(titlePanel, 0);

            StackPanel navPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(30, 0, 0, 0) };
            _tabBtnDesigner = CreateTabNavButton("准星方案与设计器", true);
            _tabBtnDesigner.Click += (s, e) => SwitchTab(true);

            _tabBtnSettings = CreateTabNavButton("主题·按键·个性化", false);
            _tabBtnSettings.Click += (s, e) => SwitchTab(false);

            navPanel.Children.Add(_tabBtnDesigner);
            navPanel.Children.Add(_tabBtnSettings);
            Grid.SetColumn(navPanel, 1);

            StackPanel topActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            _statusBadge = new TextBlock
            {
                Text = _config.OverlayEnabled ? string.Format("● 准星运行中 ({0})", _config.ToggleHotKeyName) : string.Format("○ 准星已暂停 ({0})", _config.ToggleHotKeyName),
                Foreground = _config.OverlayEnabled ? new SolidColorBrush(Color.FromRgb(52, 199, 89)) : new SolidColorBrush(Color.FromRgb(142, 142, 147)),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _btnToggleOverlay = CreateButton(_config.OverlayEnabled ? string.Format("关闭准星 ({0})", _config.ToggleHotKeyName) : string.Format("开启准星 ({0})", _config.ToggleHotKeyName), 130, 32,
                _config.OverlayEnabled ? DangerBtnColor : NeutralBtnColor);
            _btnToggleOverlay.Click += (s, e) => ToggleOverlay();

            Button btnMinimize = CreateButton("最小化到托盘", 100, 32, NeutralBtnColor);
            btnMinimize.Margin = new Thickness(8, 0, 0, 0);
            btnMinimize.Click += (s, e) => { Hide(); };

            topActions.Children.Add(_statusBadge);
            topActions.Children.Add(_btnToggleOverlay);
            topActions.Children.Add(btnMinimize);
            Grid.SetColumn(topActions, 2);

            topGrid.Children.Add(titlePanel);
            topGrid.Children.Add(navPanel);
            topGrid.Children.Add(topActions);
            _topBorder.Child = topGrid;
            Grid.SetRow(_topBorder, 0);
            rootGrid.Children.Add(_topBorder);

            // 2. Main Content Tabs
            _mainContentGrid = new Grid();

            _designerTabGrid = BuildDesignerTab();
            _mainContentGrid.Children.Add(_designerTabGrid);

            _settingsTabScroll = BuildSettingsTab();
            _settingsTabScroll.Visibility = Visibility.Collapsed;
            _mainContentGrid.Children.Add(_settingsTabScroll);

            Grid.SetRow(_mainContentGrid, 1);
            rootGrid.Children.Add(_mainContentGrid);

            // 3. Bottom Bar
            _bottomBorder = new Border
            {
                Background = _themeHeaderBrush,
                BorderBrush = _themeBorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(15, 6, 15, 6)
            };
            Grid bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel botLeftStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // ⭐ USER REQUEST: 左下角加上“免费软件 禁止盗卖”
            Border badgeFree = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 69, 58)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 69, 58)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2.5, 8, 2.5),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock txtFree = new TextBlock
            {
                Text = "🛡️ 免费软件 · 禁止盗卖",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 105, 97)),
                FontWeight = FontWeights.Bold,
                FontSize = 11.5
            };
            badgeFree.Child = txtFree;
            botLeftStack.Children.Add(badgeFree);

            ScreenMetrics initMetrics = Win32Helper.GetPrimaryScreenMetrics();
            _txtBottomStatus = new TextBlock
            {
                Text = initMetrics.DpiPercent != 100
                    ? string.Format("ScreenCrosshair v7.2 · 全局准星引擎已就绪 · 分辨率: {0}×{1} (缩放 {2}%) · by zhezhi",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.DpiPercent)
                    : string.Format("ScreenCrosshair v7.2 · 全局准星引擎已就绪 · 分辨率: {0}×{1} · by zhezhi",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            botLeftStack.Children.Add(_txtBottomStatus);
            Grid.SetColumn(botLeftStack, 0);

            StackPanel botActions = new StackPanel { Orientation = Orientation.Horizontal };
            Button btnSave = CreateThemedActionButton("💾 保存配置", 96, 28);
            btnSave.Click += (s, e) =>
            {
                SaveColumnRatios();
                _config.Save();
                MessageBox.Show("所有方案组、图层、壁纸与按键配置已成功保存！", "ScreenCrosshair v7.2", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            Button btnQuit = CreateButton("退出程序", 80, 28, NeutralBtnColor);
            btnQuit.Margin = new Thickness(8, 0, 0, 0);
            btnQuit.Click += (s, e) =>
            {
                SaveColumnRatios();
                _config.Save();
                Application.Current.Shutdown();
            };
            botActions.Children.Add(btnSave);
            botActions.Children.Add(btnQuit);
            Grid.SetColumn(botActions, 1);

            bottomGrid.Children.Add(botLeftStack);
            bottomGrid.Children.Add(botActions);
            _bottomBorder.Child = bottomGrid;
            Grid.SetRow(_bottomBorder, 2);
            rootGrid.Children.Add(_bottomBorder);

            _hostGrid.Children.Add(rootGrid);
            Content = _hostGrid;
        }

        private void SwitchTab(bool isDesigner)
        {
            _designerTabGrid.Visibility = isDesigner ? Visibility.Visible : Visibility.Collapsed;
            _settingsTabScroll.Visibility = isDesigner ? Visibility.Collapsed : Visibility.Visible;

            _tabBtnDesigner.Background = isDesigner ? _themeAccentBrush : Brushes.Transparent;
            _tabBtnDesigner.Foreground = isDesigner ? Brushes.Black : Brushes.White;

            _tabBtnSettings.Background = !isDesigner ? _themeAccentBrush : Brushes.Transparent;
            _tabBtnSettings.Foreground = !isDesigner ? Brushes.Black : Brushes.White;
        }

        // ======================= ⭐ Mouse Drag & Position Calibration =======================
    }
}
