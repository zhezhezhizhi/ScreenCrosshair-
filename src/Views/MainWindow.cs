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
    public class MainWindow : Window
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

            Title = "ScreenCrosshair v6.9";
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
                        "ScreenCrosshair v6.9 · 全局准星引擎已就绪 · 分辨率: {0}×{1} (缩放 {2}%) · by zhezhi",
                        m.PhysicalWidth, m.PhysicalHeight, m.DpiPercent);
                }
                else
                {
                    _txtBottomStatus.Text = string.Format(
                        "ScreenCrosshair v6.9 · 全局准星引擎已就绪 · 分辨率: {0}×{1} · by zhezhi",
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
            ScreenMetrics m = Win32Helper.GetPrimaryScreenMetrics();
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

                MessageBox.Show(msg, "分辨率检测成功 - ScreenCrosshair v6.9", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ComputeThemeBrushes()
        {
            string accentHex = _config.ThemeAccentHex ?? "#0A84FF";
            Color accentColor;
            try { accentColor = (Color)ColorConverter.ConvertFromString(accentHex); }
            catch { accentColor = Color.FromRgb(10, 132, 255); }

            _themeAccentBrush = new SolidColorBrush(accentColor);
            _themeAccentBrush.Freeze();

            _themeBgBrush = CrosshairRenderer.ParseBrush(_config.UiBgColorHex ?? "#1C1C1E", 1.0);
            _themeCardBrush = CrosshairRenderer.ParseBrush(_config.UiCardColorHex ?? "#2C2C2E", 1.0);
            _themeHeaderBrush = CrosshairRenderer.ParseBrush(_config.UiHeaderColorHex ?? "#242426", 1.0);
            _themeBorderBrush = CrosshairRenderer.ParseBrush(_config.UiBorderColorHex ?? "#38383A", 1.0);
            _themeInputBgBrush = CrosshairRenderer.ParseBrush(_config.UiInputBgColorHex ?? "#161618", 1.0);
            _themeInputTextBrush = CrosshairRenderer.ParseBrush(_config.UiInputTextColorHex ?? "#FFFFFF", 1.0);
            _themeButtonBrush = CrosshairRenderer.ParseBrush(_config.UiButtonBgColorHex ?? "#0A84FF", 1.0);
            _themeDangerBrush = CrosshairRenderer.ParseBrush(_config.UiDangerColorHex ?? "#FF453A", 1.0);
            _themeTextBrush = CrosshairRenderer.ParseBrush(_config.UiTextColorHex ?? "#FFFFFF", 1.0);
            _themeSubTextBrush = CrosshairRenderer.ParseBrush(_config.UiSubTextColorHex ?? "#8E8E93", 1.0);
        }

        private ControlTemplate GetComboBoxControlTemplate()
        {
            if (_comboBoxTemplateCache != null) return _comboBoxTemplateCache;
            string xaml = @"
<ControlTemplate TargetType=""{x:Type ComboBox}"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid SnapsToDevicePixels=""True"" TextElement.Foreground=""{TemplateBinding Foreground}"" TextElement.FontWeight=""SemiBold"">
        <ToggleButton Name=""ToggleButton"" Focusable=""False"" 
                      Background=""{TemplateBinding Background}"" 
                      BorderBrush=""{TemplateBinding BorderBrush}"" 
                      BorderThickness=""{TemplateBinding BorderThickness}"" 
                      Foreground=""{TemplateBinding Foreground}"" 
                      IsChecked=""{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}"" ClickMode=""Press"">
            <ToggleButton.Template>
                <ControlTemplate TargetType=""{x:Type ToggleButton}"">
                    <Border Name=""Border"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""8"">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width=""*""/>
                                <ColumnDefinition Width=""24""/>
                            </Grid.ColumnDefinitions>
                            <Path Grid.Column=""1"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Fill=""{TemplateBinding Foreground}"" Data=""M 0 0 L 4 4 L 8 0 Z""/>
                        </Grid>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Opacity"" Value=""0.88""/>
                        </Trigger>
                        <Trigger Property=""IsChecked"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Opacity"" Value=""0.95""/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </ToggleButton.Template>
        </ToggleButton>
        <ContentPresenter Name=""ContentSite"" IsHitTestVisible=""False"" Content=""{TemplateBinding SelectionBoxItem}"" ContentTemplate=""{TemplateBinding SelectionBoxItemTemplate}"" ContentTemplateSelector=""{TemplateBinding ItemTemplateSelector}"" Margin=""10,2,26,2"" VerticalAlignment=""Center"" HorizontalAlignment=""Left"">
            <ContentPresenter.Resources>
                <Style TargetType=""{x:Type TextBlock}"">
                    <Setter Property=""Foreground"" Value=""{Binding Path=Foreground, RelativeSource={RelativeSource AncestorType={x:Type ComboBox}}}""/>
                    <Setter Property=""FontWeight"" Value=""SemiBold""/>
                    <Setter Property=""FontSize"" Value=""11.5""/>
                    <Setter Property=""TextTrimming"" Value=""CharacterEllipsis""/>
                </Style>
            </ContentPresenter.Resources>
        </ContentPresenter>
        <Popup Name=""Popup"" Placement=""Bottom"" IsOpen=""{TemplateBinding IsDropDownOpen}"" AllowsTransparency=""True"" Focusable=""False"" PopupAnimation=""Slide"">
            <Grid Name=""DropDown"" SnapsToDevicePixels=""True"" MinWidth=""{TemplateBinding ActualWidth}"" MaxHeight=""{TemplateBinding MaxDropDownHeight}"">
                <Border Name=""DropDownBorder"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""1"" CornerRadius=""8"" Margin=""0,3,0,3"">
                    <Border.Effect>
                        <DropShadowEffect BlurRadius=""14"" ShadowDepth=""4"" Opacity=""0.5"" Color=""Black""/>
                    </Border.Effect>
                    <ScrollViewer Margin=""3"" SnapsToDevicePixels=""True"">
                        <StackPanel IsItemsHost=""True"" KeyboardNavigation.DirectionalNavigation=""Contained"" />
                    </ScrollViewer>
                </Border>
            </Grid>
        </Popup>
    </Grid>
</ControlTemplate>";
            _comboBoxTemplateCache = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
            return _comboBoxTemplateCache;
        }

        private void ApplyComboBoxTheming(ComboBox cbo)
        {
            if (cbo == null) return;
            try
            {
                cbo.Template = GetComboBoxControlTemplate();
                cbo.Background = _themeInputBgBrush;
                cbo.Foreground = _themeInputTextBrush;
                cbo.BorderBrush = _themeBorderBrush;
                cbo.BorderThickness = new Thickness(1);
                cbo.ItemContainerStyle = CreateThemedComboBoxItemStyle();
            }
            catch { }
        }

        private void ApplyThemeToAllControls()
        {
            ComputeThemeBrushes();

            Background = _themeBgBrush;

            if (_topBorder != null)
            {
                _topBorder.Background = _themeHeaderBrush;
                _topBorder.BorderBrush = _themeBorderBrush;
            }
            if (_bottomBorder != null)
            {
                _bottomBorder.Background = _themeHeaderBrush;
                _bottomBorder.BorderBrush = _themeBorderBrush;
            }

            foreach (Border b in _cardBorders)
            {
                b.Background = _themeCardBrush;
                b.BorderBrush = _themeBorderBrush;
                b.CornerRadius = new CornerRadius(12);
            }

            foreach (Control input in _themedInputs)
            {
                input.Background = _themeInputBgBrush;
                input.Foreground = _themeInputTextBrush;
                input.BorderBrush = _themeBorderBrush;
                input.BorderThickness = new Thickness(1);
            }

            // Apple Dark system window brushes
            this.Resources[SystemColors.WindowBrushKey] = _themeBgBrush;
            this.Resources[SystemColors.WindowTextBrushKey] = _themeTextBrush;
            this.Resources[SystemColors.HighlightBrushKey] = _themeAccentBrush;
            this.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
            this.Resources[SystemColors.ControlBrushKey] = _themeInputBgBrush;
            this.Resources[SystemColors.ControlTextBrushKey] = _themeInputTextBrush;
            this.Resources[SystemColors.WindowFrameBrushKey] = _themeBorderBrush;

            // Global Apple Rounded Button Style
            try
            {
                FrameworkElementFactory btnBorderFactory = new FrameworkElementFactory(typeof(Border), "btnBd");
                btnBorderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                btnBorderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                btnBorderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                btnBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

                FrameworkElementFactory btnContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                btnContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                btnContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                btnContentFactory.SetValue(ContentPresenter.MarginProperty, new Thickness(4, 2, 4, 2));
                btnBorderFactory.AppendChild(btnContentFactory);

                ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
                btnTemplate.VisualTree = btnBorderFactory;

                Trigger btnHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                btnHover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.85));
                btnTemplate.Triggers.Add(btnHover);

                Trigger btnPressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
                btnPressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.65));
                btnTemplate.Triggers.Add(btnPressed);

                Style btnStyle = new Style(typeof(Button));
                btnStyle.Setters.Add(new Setter(Control.TemplateProperty, btnTemplate));
                btnStyle.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
                this.Resources[typeof(Button)] = btnStyle;
            }
            catch { }

            // Global Apple Rounded TextBox Style
            try
            {
                FrameworkElementFactory txtBorderFactory = new FrameworkElementFactory(typeof(Border), "txtBd");
                txtBorderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                txtBorderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                txtBorderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                txtBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));

                FrameworkElementFactory txtContentHost = new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost");
                txtContentHost.SetValue(ScrollViewer.MarginProperty, new Thickness(4, 2, 4, 2));
                txtContentHost.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);
                txtBorderFactory.AppendChild(txtContentHost);

                ControlTemplate txtTemplate = new ControlTemplate(typeof(TextBox));
                txtTemplate.VisualTree = txtBorderFactory;

                Style txtStyle = new Style(typeof(TextBox));
                txtStyle.Setters.Add(new Setter(Control.TemplateProperty, txtTemplate));
                txtStyle.Setters.Add(new Setter(Control.BackgroundProperty, _themeInputBgBrush));
                txtStyle.Setters.Add(new Setter(Control.ForegroundProperty, _themeInputTextBrush));
                txtStyle.Setters.Add(new Setter(Control.BorderBrushProperty, _themeBorderBrush));
                txtStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                txtStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, _themeInputTextBrush));
                txtStyle.Setters.Add(new Setter(TextBox.SelectionBrushProperty, _themeAccentBrush));
                this.Resources[typeof(TextBox)] = txtStyle;
            }
            catch { }

            // Global ComboBox custom template & item styling
            try
            {
                Style cboItemStyle = CreateThemedComboBoxItemStyle();
                this.Resources[typeof(ComboBoxItem)] = cboItemStyle;

                Style cboStyle = new Style(typeof(ComboBox));
                cboStyle.Setters.Add(new Setter(Control.TemplateProperty, GetComboBoxControlTemplate()));
                cboStyle.Setters.Add(new Setter(Control.BackgroundProperty, _themeInputBgBrush));
                cboStyle.Setters.Add(new Setter(Control.ForegroundProperty, _themeInputTextBrush));
                cboStyle.Setters.Add(new Setter(Control.BorderBrushProperty, _themeBorderBrush));
                cboStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                cboStyle.Setters.Add(new Setter(ItemsControl.ItemContainerStyleProperty, cboItemStyle));
                this.Resources[typeof(ComboBox)] = cboStyle;
            }
            catch { }

            // Explicitly re-apply to all ComboBox instances to force instant refresh
            ApplyComboBoxTheming(_cboMode);
            ApplyComboBoxTheming(_cboGroups);
            ApplyComboBoxTheming(_cboPreviewBg);
            ApplyComboBoxTheming(_cboDotShape);
            ApplyComboBoxTheming(_cboThemePresets);

            foreach (TextBlock header in _sectionHeaders)
            {
                header.Foreground = _themeAccentBrush;
            }

            if (_logoText != null) _logoText.Foreground = _themeAccentBrush;
            if (_txtHkOverlayHint != null) _txtHkOverlayHint.Foreground = _themeAccentBrush;
            if (_tabBtnDesigner != null && _designerTabGrid != null && _designerTabGrid.Visibility == Visibility.Visible)
            {
                _tabBtnDesigner.Background = _themeAccentBrush;
            }
            if (_tabBtnSettings != null && _settingsTabScroll != null && _settingsTabScroll.Visibility == Visibility.Visible)
            {
                _tabBtnSettings.Background = _themeAccentBrush;
            }
            if (_zoomPillsPanel != null)
            {
                UpdateZoomPillsStyles(_zoomPillsPanel);
            }
            UpdatePreviewSwitchStyles();
            if (_splitterLeft != null) _splitterLeft.Template = CreateSplitterTemplate();
            if (_splitterRight != null) _splitterRight.Template = CreateSplitterTemplate();

            UpdateStatusBadge();
            UpdateLockStatusUI();
            RefreshLayerList();
            RefreshGroupList();
            UpdatePreview();
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
                Text = "v6.9",
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
                    ? string.Format("ScreenCrosshair v6.9 · 全局准星引擎已就绪 · 分辨率: {0}×{1} (缩放 {2}%) · by zhezhi",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.DpiPercent)
                    : string.Format("ScreenCrosshair v6.9 · 全局准星引擎已就绪 · 分辨率: {0}×{1} · by zhezhi",
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
                MessageBox.Show("所有方案组、图层、壁纸与按键配置已成功保存！", "ScreenCrosshair v6.9", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private Grid BuildDesignerTab()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(12) };

            // ⭐ USER REQUEST: 三个大界面支持拖拽自由调比例 (可记忆持久化)
            double rLeft = _config.ColumnLeftRatio > 50 ? _config.ColumnLeftRatio : 290.0;
            double rCenter = _config.ColumnCenterRatio > 50 ? _config.ColumnCenterRatio : 380.0;
            double rRight = _config.ColumnRightRatio > 50 ? _config.ColumnRightRatio : 480.0;

            _colLeft = new ColumnDefinition { Width = new GridLength(rLeft, GridUnitType.Star), MinWidth = 200 };
            ColumnDefinition colSplitLeft = new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) };
            _colCenter = new ColumnDefinition { Width = new GridLength(rCenter, GridUnitType.Star), MinWidth = 360 };
            ColumnDefinition colSplitRight = new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) };
            _colRight = new ColumnDefinition { Width = new GridLength(rRight, GridUnitType.Star), MinWidth = 260 };

            mainGrid.ColumnDefinitions.Add(_colLeft);
            mainGrid.ColumnDefinitions.Add(colSplitLeft);
            mainGrid.ColumnDefinitions.Add(_colCenter);
            mainGrid.ColumnDefinitions.Add(colSplitRight);
            mainGrid.ColumnDefinitions.Add(_colRight);

            // ---------------- Column 0: Layers & Groups Panel ----------------
            Border leftBorder = CreateCardBorder();
            Grid leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.1, GridUnitType.Star) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

            StackPanel leftHeader = new StackPanel { Margin = new Thickness(10, 8, 10, 6) };
            Grid lhGrid = new Grid();
            lhGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lhGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            TextBlock txtLayerTitle = new TextBlock
            {
                Text = "当前生效准星图层 (屏幕渲染)",
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button btnAddLayer = CreateThemedActionButton("+ 添加图层", 85, 26);
            btnAddLayer.Click += (s, e) => AddNewLayer();

            Grid.SetColumn(txtLayerTitle, 0);
            Grid.SetColumn(btnAddLayer, 1);
            lhGrid.Children.Add(txtLayerTitle);
            lhGrid.Children.Add(btnAddLayer);
            leftHeader.Children.Add(lhGrid);
            Grid.SetRow(leftHeader, 0);

            _layerListPanel = new StackPanel();
            ScrollViewer listScroll = new ScrollViewer
            {
                Content = _layerListPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(8, 0, 8, 4)
            };
            Grid.SetRow(listScroll, 1);

            // Group Box
            Border groupBox = CreateCardBorder();
            groupBox.Margin = new Thickness(6, 4, 6, 6);
            groupBox.Padding = new Thickness(8);
            Grid groupGrid = new Grid();
            groupGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            groupGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            groupGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel groupHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
            Grid ghTop = new Grid();
            ghTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ghTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtGroupTitle = new TextBlock
            {
                Text = "📁 自定义准星方案组",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = _themeAccentBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            _sectionHeaders.Add(txtGroupTitle);

            StackPanel groupBtns = new StackPanel { Orientation = Orientation.Horizontal };
            Button btnNewGroup = CreateMiniButton("+新建组", "新建自定义方案组", NeutralBtnColor);
            btnNewGroup.Width = 52;
            btnNewGroup.Click += (s, e) => CreateNewGroup();

            Button btnRenameGroup = CreateMiniButton("✎改名", "修改当前组名", NeutralBtnColor);
            btnRenameGroup.Width = 42;
            btnRenameGroup.Click += (s, e) => RenameCurrentGroup();

            Button btnDelGroup = CreateMiniButton("✕删除", "删除当前方案组", DangerBtnColor);
            btnDelGroup.Width = 42;
            btnDelGroup.Click += (s, e) => DeleteCurrentGroup();

            groupBtns.Children.Add(btnNewGroup);
            groupBtns.Children.Add(btnRenameGroup);
            groupBtns.Children.Add(btnDelGroup);

            Grid.SetColumn(txtGroupTitle, 0);
            Grid.SetColumn(groupBtns, 1);
            ghTop.Children.Add(txtGroupTitle);
            ghTop.Children.Add(groupBtns);
            groupHeader.Children.Add(ghTop);

            _cboGroups = new ComboBox { Height = 26, Margin = new Thickness(0, 4, 0, 0) };
            RegisterThemedInput(_cboGroups);
            _cboGroups.SelectionChanged += OnGroupSelectionChanged;
            groupHeader.Children.Add(_cboGroups);

            Grid.SetRow(groupHeader, 0);
            groupGrid.Children.Add(groupHeader);

            _groupLayersPanel = new StackPanel();
            ScrollViewer groupLayersScroll = new ScrollViewer
            {
                Content = _groupLayersPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 2, 0, 4)
            };
            Grid.SetRow(groupLayersScroll, 1);
            groupGrid.Children.Add(groupLayersScroll);

            StackPanel groupFlowPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            Button btnApplyGroup = CreateThemedActionButton("⚡ 应用此组全部准星 (载入屏幕)", 260, 26);
            btnApplyGroup.Click += (s, e) => ApplyGroupToScreen();

            Button btnSaveToGroup = CreateButton("💾 将当前屏幕所有准星存入此组", 260, 24, NeutralBtnColor);
            btnSaveToGroup.Margin = new Thickness(0, 4, 0, 0);
            btnSaveToGroup.Click += (s, e) => SaveScreenLayersToCurrentGroup();

            groupFlowPanel.Children.Add(btnApplyGroup);
            groupFlowPanel.Children.Add(btnSaveToGroup);
            Grid.SetRow(groupFlowPanel, 2);
            groupGrid.Children.Add(groupFlowPanel);

            groupBox.Child = groupGrid;
            Grid.SetRow(groupBox, 2);

            leftGrid.Children.Add(leftHeader);
            leftGrid.Children.Add(listScroll);
            leftGrid.Children.Add(groupBox);
            leftBorder.Child = leftGrid;
            Grid.SetColumn(leftBorder, 0);
            mainGrid.Children.Add(leftBorder);

            // ---------------- Column 1: Splitter Left (拖拽左栏与中栏比例) ----------------
            _splitterLeft = CreateThemedSplitter();
            Grid.SetColumn(_splitterLeft, 1);
            mainGrid.Children.Add(_splitterLeft);

            // ---------------- Column 2: Live Preview Area ----------------
            Border centerBorder = CreateCardBorder();
            centerBorder.Margin = new Thickness(0);
            StackPanel centerPanel = new StackPanel { Margin = new Thickness(10) };

            Grid prevHeadGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            prevHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            prevHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtPreviewTitle = new TextBlock
            {
                Text = "准星物理像素实时预览 (支持鼠标拖拽)",
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtPreviewTitle, 0);

            StackPanel bgSelectPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock lblBg = new TextBlock { Text = "测试背景: ", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            _cboPreviewBg = new ComboBox { Height = 24, Width = 115, FontSize = 11 };
            _cboPreviewBg.Items.Add("深黑纯色");
            _cboPreviewBg.Items.Add("透明棋盘格");
            _cboPreviewBg.Items.Add("沙漠灰(CF/CS)");
            _cboPreviewBg.Items.Add("雪地亮天(高光)");
            _cboPreviewBg.Items.Add("暗夜暗光");
            _cboPreviewBg.Items.Add("自定义截图...");
            _cboPreviewBg.SelectedIndex = GetBgComboIndex(_config.PreviewBackground);
            _cboPreviewBg.SelectionChanged += OnPreviewBgChanged;
            RegisterThemedInput(_cboPreviewBg);
            bgSelectPanel.Children.Add(lblBg);
            bgSelectPanel.Children.Add(_cboPreviewBg);
            Grid.SetColumn(bgSelectPanel, 1);

            prevHeadGrid.Children.Add(txtPreviewTitle);
            prevHeadGrid.Children.Add(bgSelectPanel);
            centerPanel.Children.Add(prevHeadGrid);

            // Preview Canvas Box
            _previewBox = new Border
            {
                Width = 350,
                Height = 270,
                BorderBrush = _themeBorderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true,
                Cursor = Cursors.Cross
            };
            _previewCanvas = new Canvas
            {
                Width = 350,
                Height = 270,
                SnapsToDevicePixels = false,
                UseLayoutRounding = false
            };
            _previewBox.Child = _previewCanvas;

            _previewBox.MouseDown += OnPreviewMouseDown;
            _previewBox.MouseMove += OnPreviewMouseMove;
            _previewBox.MouseUp += OnPreviewMouseUp;
            centerPanel.Children.Add(_previewBox);

            // Zoom Controls
            Border zoomBox = CreateCardBorder();
            zoomBox.Padding = new Thickness(8, 6, 8, 6);
            zoomBox.Margin = new Thickness(0, 6, 0, 0);
            StackPanel zoomStack = new StackPanel();

            Grid zoomRow1 = new Grid();
            zoomRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            zoomRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblZoom = new TextBlock
            {
                Text = "微观像素放大观察 (每个格子为1物理像素):",
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            _txtZoomVal = new TextBlock
            {
                Text = string.Format("{0}x (看像素点)", _config.PreviewZoom),
                Foreground = _themeAccentBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblZoom, 0);
            Grid.SetColumn(_txtZoomVal, 1);
            zoomRow1.Children.Add(lblZoom);
            zoomRow1.Children.Add(_txtZoomVal);
            zoomStack.Children.Add(zoomRow1);

            _zoomPillsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            int[] zoomValues = new int[] { 1, 2, 4, 8, 10, 12, 16 };
            foreach (int z in zoomValues)
            {
                int curZ = z;
                Button btnZ = new Button
                {
                    Content = string.Format("{0}x", z),
                    Width = 44,
                    Height = 22,
                    Margin = new Thickness(0, 0, 4, 0),
                    FontSize = 10.5,
                    FontWeight = curZ == 10 ? FontWeights.Bold : FontWeights.Normal,
                    Background = curZ == _config.PreviewZoom ? _themeAccentBrush : new SolidColorBrush(NeutralBtnColor),
                    Foreground = curZ == _config.PreviewZoom ? Brushes.Black : Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                btnZ.Click += (s, e) =>
                {
                    _config.PreviewZoom = curZ;
                    _txtZoomVal.Text = string.Format("{0}x (看像素点)", curZ);
                    UpdateZoomPillsStyles(_zoomPillsPanel);
                    UpdatePreview();
                };
                _zoomPillsPanel.Children.Add(btnZ);
            }
            zoomStack.Children.Add(_zoomPillsPanel);

            Grid gridChkRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            gridChkRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridChkRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _chkPixelGrid = new CheckBox
            {
                Content = "显示物理像素网格",
                IsChecked = _config.ShowPixelGrid,
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            _chkPixelGrid.Checked += (s, e) => { _config.ShowPixelGrid = true; UpdatePreview(); _config.Save(); };
            _chkPixelGrid.Unchecked += (s, e) => { _config.ShowPixelGrid = false; UpdatePreview(); _config.Save(); };
            Grid.SetColumn(_chkPixelGrid, 0);

            // Segmented pill control: 切换预览模式 (🎯 仅选中的准星 vs 👥 全部准星)
            Border segPillBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            StackPanel segPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _btnPreviewSingle = new Button
            {
                Content = "🎯 仅选中的准星",
                Height = 22,
                Margin = new Thickness(0, 0, 2, 0),
                Padding = new Thickness(7, 0, 7, 0),
                FontSize = 10.5,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            _btnPreviewSingle.Click += (s, e) =>
            {
                _config.PreviewOnlySelected = true;
                UpdatePreviewSwitchStyles();
                UpdatePreview();
                _config.Save();
            };

            _btnPreviewAll = new Button
            {
                Content = "👥 全部准星",
                Height = 22,
                Margin = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(7, 0, 7, 0),
                FontSize = 10.5,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            _btnPreviewAll.Click += (s, e) =>
            {
                _config.PreviewOnlySelected = false;
                UpdatePreviewSwitchStyles();
                UpdatePreview();
                _config.Save();
            };

            segPanel.Children.Add(_btnPreviewSingle);
            segPanel.Children.Add(_btnPreviewAll);
            segPillBox.Child = segPanel;
            Grid.SetColumn(segPillBox, 1);

            gridChkRow.Children.Add(_chkPixelGrid);
            gridChkRow.Children.Add(segPillBox);
            zoomStack.Children.Add(gridChkRow);

            zoomBox.Child = zoomStack;
            centerPanel.Children.Add(zoomBox);

            // Calibration & Mouse Drag Box
            Border offsetBox = CreateCardBorder();
            offsetBox.Padding = new Thickness(8);
            offsetBox.Margin = new Thickness(0, 6, 0, 0);
            StackPanel offsetPanel = new StackPanel();

            Grid lockTitleGrid = new Grid();
            lockTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lockTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtCalibTitle = new TextBlock
            {
                Text = "🎯 准星坐标校准 (支持鼠标拖拽 / 方向键微调)",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };

            _btnLockPosition = CreateButton(_config.IsPositionLocked ? "🔒 坐标已锁定" : "🔓 坐标已解锁", 110, 24, 
                _config.IsPositionLocked ? DangerBtnColor : NeutralBtnColor);
            _btnLockPosition.Click += (s, e) => TogglePositionLock();

            Grid.SetColumn(txtCalibTitle, 0);
            Grid.SetColumn(_btnLockPosition, 1);
            lockTitleGrid.Children.Add(txtCalibTitle);
            lockTitleGrid.Children.Add(_btnLockPosition);
            offsetPanel.Children.Add(lockTitleGrid);

            _txtLockStatus = new TextBlock
            {
                Text = _config.IsPositionLocked ? "【已锁定防误触】无法拖动，请先点击右上角解锁" : "【已解锁】可在上方预览框直接拖动，或点击下方按钮在屏幕上直接拖拽",
                Foreground = _config.IsPositionLocked ? new SolidColorBrush(Color.FromRgb(255, 120, 120)) : new SolidColorBrush(Color.FromRgb(142, 142, 147)),
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 4)
            };
            offsetPanel.Children.Add(_txtLockStatus);

            // ⭐ USER REQUEST: 屏幕直接拖拽按钮！
            Button btnScreenDrag = CreateButton("🖥️ 进入屏幕直接拖拽模式", double.NaN, 28, NeutralBtnColor);
            btnScreenDrag.HorizontalAlignment = HorizontalAlignment.Stretch;
            btnScreenDrag.Margin = new Thickness(0, 2, 0, 6);
            btnScreenDrag.ToolTip = "点击后将在物理屏幕上出现可拖拽标定手柄，直接按住拖动到枪口中心，按ESC或点击完成并锁定！";
            btnScreenDrag.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _overlayWindow.StartScreenCalibration(_selectedLayer, 
                        () => { UpdateInspectorOffsets(); UpdatePreview(); },
                        () => { UpdateInspectorOffsets(); UpdatePreview(); _config.Save(); });
                }
            };
            offsetPanel.Children.Add(btnScreenDrag);

            // X Offset (⭐ USER REQUEST: 坐标调节增加小数点三位，支持直接输入)
            Grid xGrid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            xGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            xGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            xGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            xGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock lblX = new TextBlock { Text = "水平 X 轴:", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            _txtOffsetX = new TextBox
            {
                Text = "0.000",
                Height = 24,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            RegisterThemedInput(_txtOffsetX);
            _txtOffsetX.TextChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    double val;
                    string raw = _txtOffsetX.Text.Trim().Replace(',', '.');
                    if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val))
                    {
                        _selectedLayer.OffsetX = Math.Round(val, 3);
                        _overlayWindow.SetLayers(_config.Layers);
                        _config.Save();
                    }
                }
            };
            _txtOffsetX.LostFocus += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _isUpdatingUi = true;
                    _txtOffsetX.Text = _selectedLayer.OffsetX.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                    _isUpdatingUi = false;
                }
            };
            _txtOffsetX.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            };

            TextBlock lblXUnit = new TextBlock { Text = "px", Foreground = new SolidColorBrush(Color.FromRgb(142, 142, 147)), FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) };

            StackPanel xBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            xBtns.Children.Add(CreateNudgeButton("-5", -5.0, true));
            xBtns.Children.Add(CreateNudgeButton("-1", -1.0, true));
            xBtns.Children.Add(CreateNudgeButton("-0.1", -0.1, true));
            xBtns.Children.Add(CreateNudgeButton("+0.1", 0.1, true));
            xBtns.Children.Add(CreateNudgeButton("+1", 1.0, true));
            xBtns.Children.Add(CreateNudgeButton("+5", 5.0, true));

            Grid.SetColumn(lblX, 0);
            Grid.SetColumn(_txtOffsetX, 1);
            Grid.SetColumn(lblXUnit, 2);
            Grid.SetColumn(xBtns, 3);
            xGrid.Children.Add(lblX);
            xGrid.Children.Add(_txtOffsetX);
            xGrid.Children.Add(lblXUnit);
            xGrid.Children.Add(xBtns);
            offsetPanel.Children.Add(xGrid);

            // Y Offset (⭐ USER REQUEST: 坐标调节增加小数点三位，支持直接输入)
            Grid yGrid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            yGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            yGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            yGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            yGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock lblY = new TextBlock { Text = "垂直 Y 轴:", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            _txtOffsetY = new TextBox
            {
                Text = "0.000",
                Height = 24,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            RegisterThemedInput(_txtOffsetY);
            _txtOffsetY.TextChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    double val;
                    string raw = _txtOffsetY.Text.Trim().Replace(',', '.');
                    if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val))
                    {
                        _selectedLayer.OffsetY = Math.Round(val, 3);
                        _overlayWindow.SetLayers(_config.Layers);
                        _config.Save();
                    }
                }
            };
            _txtOffsetY.LostFocus += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _isUpdatingUi = true;
                    _txtOffsetY.Text = _selectedLayer.OffsetY.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                    _isUpdatingUi = false;
                }
            };
            _txtOffsetY.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            };

            TextBlock lblYUnit = new TextBlock { Text = "px", Foreground = new SolidColorBrush(Color.FromRgb(142, 142, 147)), FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) };

            StackPanel yBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            yBtns.Children.Add(CreateNudgeButton("-5", -5.0, false));
            yBtns.Children.Add(CreateNudgeButton("-1", -1.0, false));
            yBtns.Children.Add(CreateNudgeButton("-0.1", -0.1, false));
            yBtns.Children.Add(CreateNudgeButton("+0.1", 0.1, false));
            yBtns.Children.Add(CreateNudgeButton("+1", 1.0, false));
            yBtns.Children.Add(CreateNudgeButton("+5", 5.0, false));

            Grid.SetColumn(lblY, 0);
            Grid.SetColumn(_txtOffsetY, 1);
            Grid.SetColumn(lblYUnit, 2);
            Grid.SetColumn(yBtns, 3);
            yGrid.Children.Add(lblY);
            yGrid.Children.Add(_txtOffsetY);
            yGrid.Children.Add(lblYUnit);
            yGrid.Children.Add(yBtns);
            offsetPanel.Children.Add(yGrid);

            Button btnResetOffset = CreateButton("一键居中归零 (X: 0.000, Y: 0.000)", double.NaN, 24, NeutralBtnColor);
            btnResetOffset.HorizontalAlignment = HorizontalAlignment.Stretch;
            btnResetOffset.Margin = new Thickness(0, 4, 0, 0);
            btnResetOffset.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.OffsetX = 0.0;
                    _selectedLayer.OffsetY = 0.0;
                    UpdateInspectorOffsets();
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                }
            };
            offsetPanel.Children.Add(btnResetOffset);

            // ⭐ USER REQUEST: 屏幕分辨率检测与自动居中刷新
            Border resBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid resGrid = new Grid();
            resGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            resGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            ScreenMetrics initMetrics = Win32Helper.GetPrimaryScreenMetrics();
            _lblScreenResInfo = new TextBlock
            {
                Text = initMetrics.DpiPercent != 100
                    ? string.Format("🖥️ 屏幕: {0}×{1} (物理) · {2}% 缩放 (中心点: {3}, {4})",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.DpiPercent, initMetrics.PhysicalCenterX, initMetrics.PhysicalCenterY)
                    : string.Format("🖥️ 屏幕: {0}×{1} (0,0 居中点: {2}, {3})",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.PhysicalCenterX, initMetrics.PhysicalCenterY),
                Foreground = _themeSubTextBrush ?? Brushes.White,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button btnRefreshRes = CreateButton("🔄 检查/刷新分辨率", double.NaN, 22, NeutralBtnColor);
            btnRefreshRes.FontSize = 10;
            btnRefreshRes.Padding = new Thickness(8, 0, 8, 0);
            btnRefreshRes.Click += (s, e) => CheckAndRefreshResolution(true);

            Grid.SetColumn(_lblScreenResInfo, 0);
            Grid.SetColumn(btnRefreshRes, 1);
            resGrid.Children.Add(_lblScreenResInfo);
            resGrid.Children.Add(btnRefreshRes);
            resBorder.Child = resGrid;
            offsetPanel.Children.Add(resBorder);

            offsetBox.Child = offsetPanel;
            centerPanel.Children.Add(offsetBox);

            // Hotkey Hint Box
            Border hkBox = CreateCardBorder();
            hkBox.Padding = new Thickness(8);
            hkBox.Margin = new Thickness(0, 6, 0, 0);
            StackPanel hkStack = new StackPanel();
            _txtHkOverlayHint = new TextBlock
            {
                Text = string.Format("【{0}】游戏内一键显隐准星 | 【方向键】微调坐标", _config.ToggleHotKeyName),
                Foreground = _themeAccentBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            _txtHkSettingsHint = new TextBlock
            {
                Text = string.Format("【{0}】游戏内一键呼出/隐藏设置面板 (在设置页可自定义按键)", _config.SettingsHotKeyName),
                Foreground = Brushes.White,
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            };
            hkStack.Children.Add(_txtHkOverlayHint);
            hkStack.Children.Add(_txtHkSettingsHint);
            hkBox.Child = hkStack;
            centerPanel.Children.Add(hkBox);

            centerBorder.Child = centerPanel;
            Grid.SetColumn(centerBorder, 2);
            mainGrid.Children.Add(centerBorder);

            // ---------------- Column 3: Splitter Right (拖拽中栏与右栏比例) ----------------
            _splitterRight = CreateThemedSplitter();
            Grid.SetColumn(_splitterRight, 3);
            mainGrid.Children.Add(_splitterRight);

            // ---------------- Column 4: Layer Inspector (Attributes Editor) ----------------
            Border rightBorder = CreateCardBorder();
            Grid rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock txtInspectorTitle = new TextBlock
            {
                Text = "图层属性调节 (多种样式 · 调色板 · 自定义图片准星)",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White,
                Margin = new Thickness(12, 8, 12, 4)
            };
            Grid.SetRow(txtInspectorTitle, 0);
            rightGrid.Children.Add(txtInspectorTitle);

            ScrollViewer inspScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12, 0, 12, 10)
            };
            StackPanel inspPanel = new StackPanel();

            // 1. Basic properties
            inspPanel.Children.Add(CreateSectionHeader("基础信息"));
            
            Grid nameGrid = new Grid { Margin = new Thickness(0, 2, 0, 4) };
            nameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
            nameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock lblName = new TextBlock { Text = "图层名称:", Foreground = Brushes.White, FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center };
            _txtName = new TextBox { Height = 26 };
            RegisterThemedInput(_txtName);
            _txtName.TextChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    _selectedLayer.Name = _txtName.Text;
                    RefreshLayerList();
                }
            };
            Grid.SetColumn(lblName, 0);
            Grid.SetColumn(_txtName, 1);
            nameGrid.Children.Add(lblName);
            nameGrid.Children.Add(_txtName);
            inspPanel.Children.Add(nameGrid);

            // 2. Line Mode (⭐ USER REQUEST: 丰富准星样式，加入贯通实心横线/竖线/斜十字/三角/图片准星)
            Grid modeGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock lblMode = new TextBlock { Text = "线条模式:", Foreground = _themeAccentBrush, FontWeight = FontWeights.Bold, FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center };
            _cboMode = new ComboBox { Height = 26 };
            _cboMode.Items.Add("贯通一体实心横线 (Solid Horizontal - 无中缝) ⭐");
            _cboMode.Items.Add("贯通一体实心竖线 (Solid Vertical - 无中缝) ⭐");
            _cboMode.Items.Add("贯通一体实心十字 (Solid Cross - 无中缝) ⭐");
            _cboMode.Items.Add("分段经典十字 (Both)");
            _cboMode.Items.Add("分段仅横线 (Horizontal Only)");
            _cboMode.Items.Add("分段仅竖线 (Vertical Only)");
            _cboMode.Items.Add("斜十字 / 交叉准星 (X-Cross) ⭐");
            _cboMode.Items.Add("三角人字箭标 (Chevron) ⭐");
            _cboMode.Items.Add("T字准星 (无上臂)");
            _cboMode.Items.Add("纯中心点 (Dot Only) ⭐");
            _cboMode.Items.Add("纯圆圈 (Circle Only) ⭐");
            _cboMode.Items.Add("战术括号瞄框 (Brackets)");
            _cboMode.Items.Add("自定义图片准星 (Custom PNG Image) ⭐");
            RegisterThemedInput(_cboMode);
            _cboMode.SelectionChanged += OnLineModeChanged;
            Grid.SetColumn(lblMode, 0);
            Grid.SetColumn(_cboMode, 1);
            modeGrid.Children.Add(lblMode);
            modeGrid.Children.Add(_cboMode);
            inspPanel.Children.Add(modeGrid);

            // Custom Image Reticle Panel (⭐ USER REQUEST: 支持导入图片作为准星)
            _customImageBox = CreateCardBorder();
            _customImageBox.Padding = new Thickness(8);
            _customImageBox.Margin = new Thickness(0, 2, 0, 6);
            _customImageBox.Visibility = Visibility.Collapsed;
            StackPanel imageStack = new StackPanel();

            TextBlock imgHeader = new TextBlock { Text = "🖼️ 自定义图片准星设置 (支持PNG带透明通道)", FontWeight = FontWeights.Bold, Foreground = _themeAccentBrush, FontSize = 11 };
            _sectionHeaders.Add(imgHeader);
            imageStack.Children.Add(imgHeader);

            StackPanel imgBtnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            Button btnSelectImage = CreateButton("📂 浏览选择准星图片 (PNG/JPG/ICO)...", 240, 26, NeutralBtnColor);
            btnSelectImage.Click += (s, e) =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "选择准星图片文件";
                ofd.Filter = "图片文件 (*.png;*.jpg;*.gif;*.ico;*.bmp)|*.png;*.jpg;*.gif;*.ico;*.bmp|所有文件 (*.*)|*.*";
                if (ofd.ShowDialog() == true && _selectedLayer != null)
                {
                    _selectedLayer.CustomImagePath = ofd.FileName;
                    _txtCustomImagePath.Text = ofd.FileName;
                    double ratio = GetImageAspectRatio(ofd.FileName);
                    if (_selectedLayer.LockAspectRatio && ratio > 0 && _sliderImageH != null)
                    {
                        double targetH = Math.Max(4, Math.Min(500, Math.Round(_sliderImageW.Value / ratio)));
                        _isUpdatingUi = true;
                        _sliderImageH.Value = targetH;
                        _txtImageHVal.Text = ((int)targetH).ToString();
                        _selectedLayer.ImageHeight = targetH;
                        _isUpdatingUi = false;
                    }
                    UpdateAllViews();
                    _config.Save();
                }
            };
            imgBtnRow.Children.Add(btnSelectImage);
            imageStack.Children.Add(imgBtnRow);

            _txtCustomImagePath = new TextBlock { Text = "(暂未选择图片文件)", Foreground = Brushes.White, FontSize = 10, Margin = new Thickness(0, 0, 0, 4) };
            imageStack.Children.Add(_txtCustomImagePath);

            // ⭐ USER REQUEST: 纵横比锁定功能
            _chkLockAspectRatio = new CheckBox
            {
                Content = "🔒 锁定图片宽高比 (取消勾选可自由拉伸/压扁压缩)",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 6),
                IsChecked = true
            };
            _chkLockAspectRatio.Checked += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.LockAspectRatio = true;
                    UpdateAllViews();
                }
            };
            _chkLockAspectRatio.Unchecked += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.LockAspectRatio = false;
                    UpdateAllViews();
                }
            };
            imageStack.Children.Add(_chkLockAspectRatio);

            imageStack.Children.Add(CreateSliderRow("图片宽度 (px):", 4, 500, 32, out _sliderImageW, out _txtImageWVal, val =>
            {
                if (_selectedLayer != null && !_isUpdatingUi)
                {
                    _selectedLayer.ImageWidth = val;
                    if (_selectedLayer.LockAspectRatio && _sliderImageH != null)
                    {
                        double ratio = GetImageAspectRatio(_selectedLayer.CustomImagePath);
                        if (ratio > 0)
                        {
                            double targetH = Math.Max(4, Math.Min(500, Math.Round(val / ratio)));
                            if (Math.Abs(_sliderImageH.Value - targetH) >= 1)
                            {
                                _isUpdatingUi = true;
                                _sliderImageH.Value = targetH;
                                _txtImageHVal.Text = ((int)targetH).ToString();
                                _selectedLayer.ImageHeight = targetH;
                                _isUpdatingUi = false;
                            }
                        }
                    }
                    UpdateAllViews();
                }
            }));

            imageStack.Children.Add(CreateSliderRow("图片高度 (px):", 4, 500, 32, out _sliderImageH, out _txtImageHVal, val =>
            {
                if (_selectedLayer != null && !_isUpdatingUi)
                {
                    _selectedLayer.ImageHeight = val;
                    if (_selectedLayer.LockAspectRatio && _sliderImageW != null)
                    {
                        double ratio = GetImageAspectRatio(_selectedLayer.CustomImagePath);
                        if (ratio > 0)
                        {
                            double targetW = Math.Max(4, Math.Min(500, Math.Round(val * ratio)));
                            if (Math.Abs(_sliderImageW.Value - targetW) >= 1)
                            {
                                _isUpdatingUi = true;
                                _sliderImageW.Value = targetW;
                                _txtImageWVal.Text = ((int)targetW).ToString();
                                _selectedLayer.ImageWidth = targetW;
                                _isUpdatingUi = false;
                            }
                        }
                    }
                    UpdateAllViews();
                }
            }));

            imageStack.Children.Add(CreateSliderRow("旋转角度 (°):", 0, 360, 0, out _sliderImageRot, out _txtImageRotVal, val => { if (_selectedLayer != null) { _selectedLayer.ImageRotation = val; UpdateAllViews(); } }, true));

            _customImageBox.Child = imageStack;
            inspPanel.Children.Add(_customImageBox);

            // 3. Line Sliders (⭐ USER REQUEST: 间隙支持负数 -50~300, 长度可调到500, 全部选项支持3位小数)
            inspPanel.Children.Add(CreateSectionHeader("线条尺寸参数 (全支持3位小数精确微调)"));
            inspPanel.Children.Add(CreateSliderRow("线条长度/半长 (px):", 0, 500, 10, out _sliderLength, out _txtLengthVal, val => { if (_selectedLayer != null) { _selectedLayer.Length = val; UpdateAllViews(); } }, true));
            inspPanel.Children.Add(CreateSliderRow("线条粗细 (px):", 0.1, 60, 2, out _sliderThickness, out _txtThicknessVal, val => { if (_selectedLayer != null) { _selectedLayer.Thickness = val; UpdateAllViews(); } }, true));
            inspPanel.Children.Add(CreateSliderRow("中心间隙 (可填负数! px):", -50, 300, 5, out _sliderGap, out _txtGapVal, val => { if (_selectedLayer != null) { _selectedLayer.Gap = val; UpdateAllViews(); } }, true));

            // ⭐ USER REQUEST: 可以设置准星旋转 (0.000° ~ 360.000° 次像素平滑抗锯齿旋转)
            inspPanel.Children.Add(CreateSectionHeader("准星旋转角度 (0~360° 次像素平滑抗锯齿)"));
            inspPanel.Children.Add(CreateSliderRow("准星旋转 (°):", 0, 360, 0, out _sliderRotation, out _txtRotationVal, val =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.Rotation = val;
                    UpdateAllViews();
                }
            }, true));

            StackPanel rotPresetRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 4) };
            double[] quickAngles = new double[] { 0, 45, 90, 135, 180, 270 };
            foreach (double ang in quickAngles)
            {
                Button btnAng = CreateMiniButton(string.Format("{0}°", (int)ang), string.Format("一键旋转至 {0} 度", (int)ang), NeutralBtnColor);
                btnAng.Width = 38;
                btnAng.Height = 22;
                double targetAng = ang;
                btnAng.Click += (s, e) =>
                {
                    if (_selectedLayer != null)
                    {
                        _selectedLayer.Rotation = targetAng;
                        _sliderRotation.Value = targetAng;
                        _txtRotationVal.Text = targetAng.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                        UpdateAllViews();
                    }
                };
                rotPresetRow.Children.Add(btnAng);
            }
            inspPanel.Children.Add(rotPresetRow);

            inspPanel.Children.Add(CreateSliderRow("不透明度 (%):", 0, 100, 100, out _sliderAlpha, out _txtAlphaVal, val => { if (_selectedLayer != null) { _selectedLayer.Alpha = val / 100.0; UpdateAllViews(); } }, true));

            // 4. Color Palette (⭐ USER REQUEST: 直接加入系统级调色板按钮！)
            inspPanel.Children.Add(CreateSectionHeader("主线条颜色 (支持一键调色板)"));
            StackPanel colorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 4) };
            colorRow.Children.Add(CreatePaletteButton("#00FF00"));
            colorRow.Children.Add(CreatePaletteButton("#FF2222"));
            colorRow.Children.Add(CreatePaletteButton("#00E5FF"));
            colorRow.Children.Add(CreatePaletteButton("#FFFF00"));
            colorRow.Children.Add(CreatePaletteButton("#E040FB"));
            colorRow.Children.Add(CreatePaletteButton("#FFB6C1"));
            colorRow.Children.Add(CreatePaletteButton("#FFFFFF"));

            Button btnLineColorDlg = CreateButton("🎨 调色板...", 75, 24, NeutralBtnColor);
            btnLineColorDlg.Margin = new Thickness(4, 0, 0, 0);
            btnLineColorDlg.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    OpenColorPicker(_selectedLayer.ColorHex, hex =>
                    {
                        _selectedLayer.ColorHex = hex;
                        _txtColorHex.Text = hex;
                        UpdateAllViews();
                    });
                }
            };
            colorRow.Children.Add(btnLineColorDlg);
            
            _txtColorHex = new TextBox { Width = 70, Height = 24, Margin = new Thickness(6, 0, 0, 0) };
            RegisterThemedInput(_txtColorHex);
            _txtColorHex.TextChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null && _txtColorHex.Text.Length >= 4)
                {
                    _selectedLayer.ColorHex = _txtColorHex.Text;
                    UpdateAllViews();
                }
            };
            colorRow.Children.Add(_txtColorHex);
            inspPanel.Children.Add(colorRow);

            // 5. Outline Settings
            inspPanel.Children.Add(CreateSectionHeader("黑色高对比度轮廓 (防背景吃准星)"));
            _chkOutline = new CheckBox { Content = "开启黑色描边轮廓", Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) };
            _chkOutline.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowOutline = true; UpdateAllViews(); } };
            _chkOutline.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowOutline = false; UpdateAllViews(); } };
            inspPanel.Children.Add(_chkOutline);
            inspPanel.Children.Add(CreateSliderRow("描边粗细 (px):", 0.1, 20, 1, out _sliderOutline, out _txtOutlineVal, val => { if (_selectedLayer != null) { _selectedLayer.OutlineThickness = val; UpdateAllViews(); } }, true));

            // 6. Center Dot Settings
            inspPanel.Children.Add(CreateSectionHeader("中心瞄准点设置 (物理级像素对齐)"));
            _chkDot = new CheckBox { Content = "启用中心点", Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) };
            _chkDot.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowDot = true; UpdateAllViews(); } };
            _chkDot.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowDot = false; UpdateAllViews(); } };
            inspPanel.Children.Add(_chkDot);

            Grid dotShapeGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            dotShapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            dotShapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock lblDotShape = new TextBlock { Text = "点形状模式:", Foreground = _themeAccentBrush, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            _cboDotShape = new ComboBox { Height = 24 };
            _cboDotShape.Items.Add("方形像素点 (Square - 推荐)");
            _cboDotShape.Items.Add("抗锯齿圆点 (Circle)");
            _cboDotShape.SelectionChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    _selectedLayer.DotShape = _cboDotShape.SelectedIndex == 0 ? "Square" : "Circle";
                    UpdateAllViews();
                }
            };
            RegisterThemedInput(_cboDotShape);
            Grid.SetColumn(lblDotShape, 0);
            Grid.SetColumn(_cboDotShape, 1);
            dotShapeGrid.Children.Add(lblDotShape);
            dotShapeGrid.Children.Add(_cboDotShape);
            inspPanel.Children.Add(dotShapeGrid);

            inspPanel.Children.Add(CreateSliderRow("点尺寸 (px):", 0.1, 60, 2, out _sliderDotSize, out _txtDotSizeVal, val => { if (_selectedLayer != null) { _selectedLayer.DotSize = val; UpdateAllViews(); } }, true));

            StackPanel dotColorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 4) };
            dotColorRow.Children.Add(new TextBlock { Text = "点颜色: ", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
            dotColorRow.Children.Add(CreateDotColorButton("#FF2222"));
            dotColorRow.Children.Add(CreateDotColorButton("#00FF00"));
            dotColorRow.Children.Add(CreateDotColorButton("#00E5FF"));
            dotColorRow.Children.Add(CreateDotColorButton("#FFB6C1"));
            dotColorRow.Children.Add(CreateDotColorButton("#FFFFFF"));

            Button btnDotColorDlg = CreateButton("🎨 调色板...", 72, 22, NeutralBtnColor);
            btnDotColorDlg.Margin = new Thickness(4, 0, 0, 0);
            btnDotColorDlg.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    OpenColorPicker(_selectedLayer.DotColorHex, hex =>
                    {
                        _selectedLayer.DotColorHex = hex;
                        _txtDotColor.Text = hex;
                        UpdateAllViews();
                    });
                }
            };
            dotColorRow.Children.Add(btnDotColorDlg);

            _txtDotColor = new TextBox { Width = 65, Height = 22, Margin = new Thickness(6, 0, 0, 0) };
            RegisterThemedInput(_txtDotColor);
            _txtDotColor.TextChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null && _txtDotColor.Text.Length >= 4)
                {
                    _selectedLayer.DotColorHex = _txtDotColor.Text;
                    UpdateAllViews();
                }
            };
            dotColorRow.Children.Add(_txtDotColor);

            _chkDotOutline = new CheckBox { Content = "点描边", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            _chkDotOutline.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.DotOutline = true; UpdateAllViews(); } };
            _chkDotOutline.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.DotOutline = false; UpdateAllViews(); } };
            dotColorRow.Children.Add(_chkDotOutline);
            inspPanel.Children.Add(dotColorRow);

            // 7. Center Circle Settings
            inspPanel.Children.Add(CreateSectionHeader("中心瞄准圆环设置"));
            _chkCircle = new CheckBox { Content = "启用中心圆环", Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) };
            _chkCircle.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowCircle = true; UpdateAllViews(); } };
            _chkCircle.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowCircle = false; UpdateAllViews(); } };
            inspPanel.Children.Add(_chkCircle);
            inspPanel.Children.Add(CreateSliderRow("圆环半径 (px):", 0.5, 500, 20, out _sliderCircleRadius, out _txtCircleRadiusVal, val => { if (_selectedLayer != null) { _selectedLayer.CircleRadius = val; UpdateAllViews(); } }, true));
            inspPanel.Children.Add(CreateSliderRow("圆环粗细 (px):", 0.1, 50, 2, out _sliderCircleThick, out _txtCircleThickVal, val => { if (_selectedLayer != null) { _selectedLayer.CircleThickness = val; UpdateAllViews(); } }, true));

            StackPanel circleColorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
            circleColorRow.Children.Add(new TextBlock { Text = "圆环颜色: ", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
            circleColorRow.Children.Add(CreateCircleColorButton("#00FF00"));
            circleColorRow.Children.Add(CreateCircleColorButton("#00E5FF"));
            circleColorRow.Children.Add(CreateCircleColorButton("#FF2222"));
            circleColorRow.Children.Add(CreateCircleColorButton("#FFFF00"));
            circleColorRow.Children.Add(CreateCircleColorButton("#FFB6C1"));

            Button btnCircleColorDlg = CreateButton("🎨 调色板...", 72, 22, NeutralBtnColor);
            btnCircleColorDlg.Margin = new Thickness(4, 0, 0, 0);
            btnCircleColorDlg.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    OpenColorPicker(_selectedLayer.CircleColorHex, hex =>
                    {
                        _selectedLayer.CircleColorHex = hex;
                        _txtCircleColor.Text = hex;
                        UpdateAllViews();
                    });
                }
            };
            circleColorRow.Children.Add(btnCircleColorDlg);

            _txtCircleColor = new TextBox { Width = 65, Height = 22, Margin = new Thickness(6, 0, 0, 0) };
            RegisterThemedInput(_txtCircleColor);
            _txtCircleColor.TextChanged += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null && _txtCircleColor.Text.Length >= 4)
                {
                    _selectedLayer.CircleColorHex = _txtCircleColor.Text;
                    UpdateAllViews();
                }
            };
            circleColorRow.Children.Add(_txtCircleColor);
            inspPanel.Children.Add(circleColorRow);

            // 8. Mouse Button Auto-Hide for Selected Layer (⭐ USER REQUEST: 准星调整里只对勾选的单一图层隐藏)
            inspPanel.Children.Add(CreateSectionHeader("鼠标按键动态隐藏 (仅对当前图层生效)"));
            StackPanel layerMouseStack = new StackPanel { Margin = new Thickness(0, 2, 0, 8) };

            _chkLayerHideOnLeftMouse = new CheckBox
            {
                Content = "按住鼠标左键时隐藏此图层 (开火射击隐藏)",
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 4),
                ToolTip = "按住鼠标左键开火射击时，仅自动隐藏当前选中的图层，其他未勾选图层正常显示"
            };
            _chkLayerHideOnLeftMouse.Checked += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    _selectedLayer.HideOnLeftMouse = true;
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                }
            };
            _chkLayerHideOnLeftMouse.Unchecked += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    _selectedLayer.HideOnLeftMouse = false;
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                }
            };
            layerMouseStack.Children.Add(_chkLayerHideOnLeftMouse);

            _chkLayerHideOnRightMouse = new CheckBox
            {
                Content = "按住鼠标右键时隐藏此图层 (开镜瞄准隐藏)",
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 4),
                ToolTip = "按住鼠标右键开镜瞄准(ADS)时，仅自动隐藏当前选中的图层，其他未勾选图层正常显示"
            };
            _chkLayerHideOnRightMouse.Checked += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    _selectedLayer.HideOnRightMouse = true;
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                }
            };
            _chkLayerHideOnRightMouse.Unchecked += (s, e) =>
            {
                if (!_isUpdatingUi && _selectedLayer != null)
                {
                    _selectedLayer.HideOnRightMouse = false;
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                }
            };
            layerMouseStack.Children.Add(_chkLayerHideOnRightMouse);
            inspPanel.Children.Add(layerMouseStack);

            inspScroll.Content = inspPanel;
            Grid.SetRow(inspScroll, 1);
            rightGrid.Children.Add(inspScroll);

            rightBorder.Child = rightGrid;
            Grid.SetColumn(rightBorder, 4);
            mainGrid.Children.Add(rightBorder);

            return mainGrid;
        }

        private ScrollViewer BuildSettingsTab()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(25, 15, 25, 20)
            };

            StackPanel settingsPanel = new StackPanel();

            // 1. Hotkey settings
            settingsPanel.Children.Add(CreateSettingsCard(
                "⚡ 全局快捷键自定义 (支持任意按键录制)",
                "无需切屏，可在游戏中直接按下快捷键触发。点击按钮后，直接按下键盘上你想绑定的任意按键即可！",
                BuildHotkeySettingsContent()
            ));

            // 2. Custom Theme & All UI Colors (⭐ USER REQUEST: 下拉框高对比度修复与全UI元素独立自定义色彩)
            settingsPanel.Children.Add(CreateSettingsCard(
                "🎨 UI 全局元素色彩深度自定义 (含下拉框高对比度修复与11大UI元素调色板)",
                "全面支持软件主背景、卡片底色、顶栏底栏、边框线条、下拉框底色与文字、高亮强调色、按钮及文字全套11种UI部件独立调色！彻底告别低对比度看不清的问题。",
                BuildThemeSettingsContent()
            ));

            // 3. Custom Software Background Wallpaper (⭐ USER REQUEST: 软件主窗口背景壁纸自定义)
            settingsPanel.Children.Add(CreateSettingsCard(
                "🖼️ 软件主界面自定义背景壁纸 (Window Wallpaper)",
                "可以导入任意你喜欢的壁纸、游戏海报或风景图作为整个 ScreenCrosshair 软件的主窗口背景，并自由调节不透明度。",
                BuildWindowWallpaperContent()
            ));

            // 4. Screen Resolution & Auto Centering (⭐ USER REQUEST: 检查屏幕分辨率与变动时自动居中)
            settingsPanel.Children.Add(CreateSettingsCard(
                "🖥️ 屏幕物理分辨率检测与准星自动居中 (0,0 居中防偏移)",
                "实时监控屏幕物理分辨率变化。当在 Windows 或全屏游戏中切换分辨率时，准星视口自动重新对齐绝对正中心。",
                BuildResolutionSettingsContent()
            ));

            // 5. Config Import & Export
            settingsPanel.Children.Add(CreateSettingsCard(
                "💾 配置文件导入与导出",
                "将您的专属多层准星与方案组配置导出为 JSON 文件分享给好友，或从好友处导入配置。",
                BuildConfigManagerContent()
            ));

            // 6. Auto Start
            settingsPanel.Children.Add(CreateSettingsCard(
                "🚀 系统运行与开机自启动",
                "设置开机时是否自动在系统后台静默启动准星大师。",
                BuildAutoStartContent()
            ));

            scroll.Content = settingsPanel;
            return scroll;
        }

        private UIElement BuildResolutionSettingsContent()
        {
            StackPanel p = new StackPanel();

            ScreenMetrics initMetrics = Win32Helper.GetPrimaryScreenMetrics();
            _lblSettingsResInfo = new TextBlock
            {
                Text = initMetrics.DpiPercent != 100
                    ? string.Format("物理屏幕分辨率: {0} × {1}  |  Windows 系统缩放: {2}%\n绝对物理中心点: X = {3} px, Y = {4} px  (工作区视口: {5} × {6})",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.DpiPercent, initMetrics.PhysicalCenterX, initMetrics.PhysicalCenterY, (int)initMetrics.DipWidth, (int)initMetrics.DipHeight)
                    : string.Format("物理屏幕分辨率: {0} × {1}  |  绝对物理中心点: X = {2} px, Y = {3} px",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.PhysicalCenterX, initMetrics.PhysicalCenterY),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 6)
            };
            p.Children.Add(_lblSettingsResInfo);

            _chkAutoRefreshRes = new CheckBox
            {
                Content = "当屏幕分辨率变动时自动刷新软件与准星 (保证 0, 0 永远在屏幕中心)",
                IsChecked = _config.AutoRefreshOnResolutionChange,
                Foreground = Brushes.White,
                FontSize = 11.5,
                Margin = new Thickness(0, 4, 0, 8)
            };
            _chkAutoRefreshRes.Checked += (s, e) =>
            {
                _config.AutoRefreshOnResolutionChange = true;
                if (_overlayWindow != null) _overlayWindow.AutoRefreshEnabled = true;
                _config.Save();
            };
            _chkAutoRefreshRes.Unchecked += (s, e) =>
            {
                _config.AutoRefreshOnResolutionChange = false;
                if (_overlayWindow != null) _overlayWindow.AutoRefreshEnabled = false;
                _config.Save();
            };
            p.Children.Add(_chkAutoRefreshRes);

            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 4) };
            Button btnManualCheck = CreateThemedActionButton("🔄 立即检查并重新校准中心", double.NaN, 28);
            btnManualCheck.Padding = new Thickness(14, 0, 14, 0);
            btnManualCheck.Click += (s, e) => CheckAndRefreshResolution(true);
            btnRow.Children.Add(btnManualCheck);

            p.Children.Add(btnRow);

            TextBlock desc = new TextBlock
            {
                Text = "💡 说明：当您在游戏中切换分辨率（例如 4:3 拉伸、1080P/2K/4K 互切）或插拔显示器时，Windows 会广播分辨率变动事件。开启此功能后，准星大师会在毫秒级内自动捕捉变动并重设窗口视口，确保绝对中心 (0.000, 0.000) 始终严丝合缝对准您的准心与枪口，绝无任何偏移。",
                Foreground = _themeSubTextBrush ?? new SolidColorBrush(Color.FromRgb(142, 142, 147)),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            p.Children.Add(desc);

            return p;
        }

        private UIElement BuildHotkeySettingsContent()
        {
            StackPanel p = new StackPanel();

            Grid r1 = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock lbl1 = new TextBlock { Text = "准星开启/隐藏切换键:", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _btnRecordOverlayKey = CreateButton(string.Format("【{0}】点击更改", _config.ToggleHotKeyName), 160, 30, NeutralBtnColor);
            _btnRecordOverlayKey.Click += (s, e) =>
            {
                _isRecordingOverlayKey = true;
                _isRecordingSettingsKey = false;
                _btnRecordOverlayKey.Content = "[请按下任意键盘按键...]";
                _btnRecordOverlayKey.Background = new SolidColorBrush(DangerBtnColor);
            };
            TextBlock tip1 = new TextBlock { Text = "默认按键为 F8，支持 F1~F12, CapsLock, Insert, Home, 字母键等", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };
            Grid.SetColumn(lbl1, 0);
            Grid.SetColumn(_btnRecordOverlayKey, 1);
            Grid.SetColumn(tip1, 2);
            r1.Children.Add(lbl1);
            r1.Children.Add(_btnRecordOverlayKey);
            r1.Children.Add(tip1);
            p.Children.Add(r1);

            Grid r2 = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock lbl2 = new TextBlock { Text = "控制面板呼出/隐藏键:", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _btnRecordSettingsKey = CreateButton(string.Format("【{0}】点击更改", _config.SettingsHotKeyName), 160, 30, NeutralBtnColor);
            _btnRecordSettingsKey.Click += (s, e) =>
            {
                _isRecordingSettingsKey = true;
                _isRecordingOverlayKey = false;
                _btnRecordSettingsKey.Content = "[请按下任意键盘按键...]";
                _btnRecordSettingsKey.Background = new SolidColorBrush(DangerBtnColor);
            };
            TextBlock tip2 = new TextBlock { Text = "默认按键为 F10，游戏中按下可立即调出本面板", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };
            Grid.SetColumn(lbl2, 0);
            Grid.SetColumn(_btnRecordSettingsKey, 1);
            Grid.SetColumn(tip2, 2);
            r2.Children.Add(lbl2);
            r2.Children.Add(_btnRecordSettingsKey);
            r2.Children.Add(tip2);
            p.Children.Add(r2);

            Button btnResetHk = CreateButton("恢复默认快捷键 (F8 / F10)", 180, 28, NeutralBtnColor);
            btnResetHk.Margin = new Thickness(0, 4, 0, 0);
            btnResetHk.Click += (s, e) =>
            {
                _config.ToggleHotKeyVK = 0x77;
                _config.ToggleHotKeyName = "F8";
                _config.SettingsHotKeyVK = 0x79;
                _config.SettingsHotKeyName = "F10";
                if (_hotKeyManager != null)
                {
                    _hotKeyManager.Register(_config.ToggleHotKeyVK, 0, _config.SettingsHotKeyVK, 0);
                }
                _btnRecordOverlayKey.Content = "【F8】点击更改";
                _btnRecordSettingsKey.Content = "【F10】点击更改";
                UpdateStatusBadge();
                _config.Save();
                MessageBox.Show("快捷键已恢复默认：【F8 开关准星】 【F10 呼出面板】", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            p.Children.Add(btnResetHk);

            return p;
        }

        private UIElement CreateUiColorRow(string title, Func<string> getter, Action<string> setter, Action onColorChanged)
        {
            Border rowBd = new Border
            {
                Background = CrosshairRenderer.ParseBrush("#222226", 0.75),
                BorderBrush = CrosshairRenderer.ParseBrush("#383842", 0.8),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(4, 3, 4, 3)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lbl = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            string initialColor = getter() ?? "#FFFFFF";

            Border swatch = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(6, 0, 6, 0),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1.2),
                Background = CrosshairRenderer.ParseBrush(initialColor, 1.0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "点击直接调色"
            };

            TextBox txt = new TextBox
            {
                Width = 75,
                Height = 25,
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas, Courier New"),
                Text = initialColor,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            RegisterThemedInput(txt);

            bool updatingInternal = false;

            txt.TextChanged += (s, e) =>
            {
                if (updatingInternal) return;
                string val = txt.Text.Trim();
                if (val.Length >= 4 && val.StartsWith("#"))
                {
                    try
                    {
                        setter(val);
                        swatch.Background = CrosshairRenderer.ParseBrush(val, 1.0);
                        if (onColorChanged != null) onColorChanged();
                    }
                    catch { }
                }
            };

            Action openPicker = () =>
            {
                OpenColorPicker(getter(), newHex =>
                {
                    updatingInternal = true;
                    setter(newHex);
                    txt.Text = newHex;
                    swatch.Background = CrosshairRenderer.ParseBrush(newHex, 1.0);
                    updatingInternal = false;
                    if (onColorChanged != null) onColorChanged();
                });
            };

            swatch.MouseLeftButtonDown += (s, e) => openPicker();

            Button btnPick = CreateButton("🎨 选色", 58, 25, NeutralBtnColor);
            btnPick.FontSize = 11;
            btnPick.Click += (s, e) => openPicker();

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(swatch, 1);
            Grid.SetColumn(txt, 2);
            Grid.SetColumn(btnPick, 3);

            g.Children.Add(lbl);
            g.Children.Add(swatch);
            g.Children.Add(txt);
            g.Children.Add(btnPick);

            rowBd.Child = g;

            _uiColorEditors.Add(new UiColorEditorItem
            {
                Title = title,
                Swatch = swatch,
                TxtHex = txt,
                Getter = getter,
                Setter = setter
            });

            return rowBd;
        }

        private void SyncUiColorInputs()
        {
            foreach (var item in _uiColorEditors)
            {
                try
                {
                    string color = item.Getter() ?? "#FFFFFF";
                    if (item.TxtHex != null && item.TxtHex.Text != color)
                    {
                        item.TxtHex.Text = color;
                    }
                    if (item.Swatch != null)
                    {
                        item.Swatch.Background = CrosshairRenderer.ParseBrush(color, 1.0);
                    }
                }
                catch { }
            }
        }

        private void ApplyThemePreset(string preset)
        {
            switch (preset)
            {
                case "AppleDark":
                    _config.ThemePreset = "AppleDark";
                    _config.UiBgColorHex = "#1C1C1E";
                    _config.UiCardColorHex = "#2C2C2E";
                    _config.UiHeaderColorHex = "#242426";
                    _config.UiBorderColorHex = "#3A3A3E";
                    _config.UiInputBgColorHex = "#161618";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#0A84FF";
                    _config.UiButtonBgColorHex = "#0A84FF";
                    _config.UiDangerColorHex = "#FF453A";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#A0A0B0";
                    break;
                case "AppleMint":
                    _config.ThemePreset = "AppleMint";
                    _config.UiBgColorHex = "#161C18";
                    _config.UiCardColorHex = "#222C25";
                    _config.UiHeaderColorHex = "#1D2620";
                    _config.UiBorderColorHex = "#334237";
                    _config.UiInputBgColorHex = "#121A15";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#30D158";
                    _config.UiButtonBgColorHex = "#30D158";
                    _config.UiDangerColorHex = "#FF453A";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#98B5A0";
                    break;
                case "AppleOrange":
                    _config.ThemePreset = "AppleOrange";
                    _config.UiBgColorHex = "#1F1A16";
                    _config.UiCardColorHex = "#2E2620";
                    _config.UiHeaderColorHex = "#26201B";
                    _config.UiBorderColorHex = "#42382F";
                    _config.UiInputBgColorHex = "#1A1512";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#FF9F0A";
                    _config.UiButtonBgColorHex = "#FF9F0A";
                    _config.UiDangerColorHex = "#FF453A";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#B5A496";
                    break;
                case "ApplePurple":
                    _config.ThemePreset = "ApplePurple";
                    _config.UiBgColorHex = "#1C1722";
                    _config.UiCardColorHex = "#2A2333";
                    _config.UiHeaderColorHex = "#231D2B";
                    _config.UiBorderColorHex = "#3E344B";
                    _config.UiInputBgColorHex = "#17121C";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#BF5AF2";
                    _config.UiButtonBgColorHex = "#BF5AF2";
                    _config.UiDangerColorHex = "#FF453A";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#A89CB5";
                    break;
                case "AppleSilver":
                    _config.ThemePreset = "AppleSilver";
                    _config.UiBgColorHex = "#1E2024";
                    _config.UiCardColorHex = "#2B2E33";
                    _config.UiHeaderColorHex = "#23262B";
                    _config.UiBorderColorHex = "#3B4048";
                    _config.UiInputBgColorHex = "#181A1D";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#64D2FF";
                    _config.UiButtonBgColorHex = "#64D2FF";
                    _config.UiDangerColorHex = "#FF453A";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#9AA2B0";
                    break;
                case "CyberNeon":
                    _config.ThemePreset = "CyberNeon";
                    _config.UiBgColorHex = "#0A0D14";
                    _config.UiCardColorHex = "#121824";
                    _config.UiHeaderColorHex = "#0D131F";
                    _config.UiBorderColorHex = "#1E293B";
                    _config.UiInputBgColorHex = "#0B111E";
                    _config.UiInputTextColorHex = "#00FFFF";
                    _config.ThemeAccentHex = "#00E5FF";
                    _config.UiButtonBgColorHex = "#00B4D8";
                    _config.UiDangerColorHex = "#FF2E93";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#8892B0";
                    break;
                case "CrimsonFury":
                    _config.ThemePreset = "CrimsonFury";
                    _config.UiBgColorHex = "#140A0D";
                    _config.UiCardColorHex = "#221117";
                    _config.UiHeaderColorHex = "#1D0D13";
                    _config.UiBorderColorHex = "#3B1B26";
                    _config.UiInputBgColorHex = "#180B10";
                    _config.UiInputTextColorHex = "#FF7777";
                    _config.ThemeAccentHex = "#FF453A";
                    _config.UiButtonBgColorHex = "#D32F2F";
                    _config.UiDangerColorHex = "#FF1744";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#B598A0";
                    break;
                case "EsportsEmerald":
                    _config.ThemePreset = "EsportsEmerald";
                    _config.UiBgColorHex = "#0D1410";
                    _config.UiCardColorHex = "#14221A";
                    _config.UiHeaderColorHex = "#101D16";
                    _config.UiBorderColorHex = "#1E3B2C";
                    _config.UiInputBgColorHex = "#0B1911";
                    _config.UiInputTextColorHex = "#55FF99";
                    _config.ThemeAccentHex = "#00E676";
                    _config.UiButtonBgColorHex = "#00C853";
                    _config.UiDangerColorHex = "#FF3D00";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#98B5A5";
                    break;
                case "GoldenVanguard":
                    _config.ThemePreset = "GoldenVanguard";
                    _config.UiBgColorHex = "#141208";
                    _config.UiCardColorHex = "#241F10";
                    _config.UiHeaderColorHex = "#1E1A0C";
                    _config.UiBorderColorHex = "#3D3418";
                    _config.UiInputBgColorHex = "#161308";
                    _config.UiInputTextColorHex = "#FFE57F";
                    _config.ThemeAccentHex = "#FFD600";
                    _config.UiButtonBgColorHex = "#FFC107";
                    _config.UiDangerColorHex = "#FF5252";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#B8B095";
                    break;
                case "HighContrastBlack":
                    _config.ThemePreset = "HighContrastBlack";
                    _config.UiBgColorHex = "#000000";
                    _config.UiCardColorHex = "#101010";
                    _config.UiHeaderColorHex = "#050505";
                    _config.UiBorderColorHex = "#404040";
                    _config.UiInputBgColorHex = "#000000";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#00FF66";
                    _config.UiButtonBgColorHex = "#1E88E5";
                    _config.UiDangerColorHex = "#FF3333";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#CCCCCC";
                    break;
                case "ApplePink":
                    _config.ThemePreset = "ApplePink";
                    _config.UiBgColorHex = "#1F181B";
                    _config.UiCardColorHex = "#2E2227";
                    _config.UiHeaderColorHex = "#261C20";
                    _config.UiBorderColorHex = "#44323A";
                    _config.UiInputBgColorHex = "#1A1316";
                    _config.UiInputTextColorHex = "#FFFFFF";
                    _config.ThemeAccentHex = "#FF80AB";
                    _config.UiButtonBgColorHex = "#FF80AB";
                    _config.UiDangerColorHex = "#FF453A";
                    _config.UiTextColorHex = "#FFFFFF";
                    _config.UiSubTextColorHex = "#C4A5B0";
                    break;
            }

            SyncUiColorInputs();
            ApplyThemeToAllControls();
            _config.Save();
        }

        private UIElement BuildThemeSettingsContent()
        {
            _uiColorEditors.Clear();
            StackPanel p = new StackPanel();

            // Row 1: Preset dropdown
            Grid r1 = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock lbl1 = new TextBlock { Text = "预设高对比风格:", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _cboThemePresets = new ComboBox { Height = 28 };
            _cboThemePresets.Items.Add("苹果深空灰 (Apple Dark - 极简深灰)");
            _cboThemePresets.Items.Add("苹果薄荷绿 (Apple Mint - 极简绿)");
            _cboThemePresets.Items.Add("苹果日落橙 (Apple Orange - 晨曦橙)");
            _cboThemePresets.Items.Add("苹果暗夜紫 (Apple Purple - 薰衣草紫)");
            _cboThemePresets.Items.Add("苹果钛金银 (Apple Silver - 银白金属)");
            _cboThemePresets.Items.Add("赛博霓虹 (Cyber Neon - 极光青)");
            _cboThemePresets.Items.Add("热血战魂 (Crimson Fury - 极速红)");
            _cboThemePresets.Items.Add("翡翠绿 (Esports Emerald - 竞技绿)");
            _cboThemePresets.Items.Add("黄金先锋 (Golden Vanguard - 辉煌金)");
            _cboThemePresets.Items.Add("苹果樱花粉 (Apple Blossom - 浪漫淡粉)");
            _cboThemePresets.SelectedIndex = GetThemePresetIndex(_config.ThemePreset);
            RegisterThemedInput(_cboThemePresets);
            _cboThemePresets.SelectionChanged += (s, e) =>
            {
                switch (_cboThemePresets.SelectedIndex)
                {
                    case 0: ApplyThemePreset("AppleDark"); break;
                    case 1: ApplyThemePreset("AppleMint"); break;
                    case 2: ApplyThemePreset("AppleOrange"); break;
                    case 3: ApplyThemePreset("ApplePurple"); break;
                    case 4: ApplyThemePreset("AppleSilver"); break;
                    case 5: ApplyThemePreset("CyberNeon"); break;
                    case 6: ApplyThemePreset("CrimsonFury"); break;
                    case 7: ApplyThemePreset("EsportsEmerald"); break;
                    case 8: ApplyThemePreset("GoldenVanguard"); break;
                    case 9: ApplyThemePreset("ApplePink"); break;
                }
            };
            Grid.SetColumn(lbl1, 0);
            Grid.SetColumn(_cboThemePresets, 1);
            r1.Children.Add(lbl1);
            r1.Children.Add(_cboThemePresets);
            p.Children.Add(r1);

            // Quick preset pills
            WrapPanel pillPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            Action<string, string, Color> addPill = (title, presetKey, pillColor) =>
            {
                Button btn = CreateButton(title, double.NaN, 26, pillColor);
                btn.Padding = new Thickness(10, 2, 10, 2);
                btn.Margin = new Thickness(0, 0, 6, 6);
                btn.Click += (s, e) =>
                {
                    ApplyThemePreset(presetKey);
                    _cboThemePresets.SelectedIndex = GetThemePresetIndex(presetKey);
                };
                pillPanel.Children.Add(btn);
            };
            addPill("🍎 苹果深空灰", "AppleDark", Color.FromRgb(44, 44, 46));
            addPill("🌸 苹果樱花粉", "ApplePink", Color.FromRgb(225, 95, 145));
            addPill("⚡ 赛博极光青", "CyberNeon", Color.FromRgb(0, 160, 190));
            addPill("🌿 翡翠竞技绿", "EsportsEmerald", Color.FromRgb(0, 150, 70));
            addPill("🔥 热血战魂红", "CrimsonFury", Color.FromRgb(190, 40, 40));
            addPill("🌙 暗夜薰衣紫", "ApplePurple", Color.FromRgb(140, 60, 180));
            addPill("🍊 晨曦日落橙", "AppleOrange", Color.FromRgb(190, 100, 10));
            p.Children.Add(pillPanel);

            // Subtitle for individual color customizer
            Border sepHeader = new Border
            {
                Background = CrosshairRenderer.ParseBrush("#18181A", 0.9),
                BorderBrush = CrosshairRenderer.ParseBrush("#303036", 0.8),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 4, 0, 8),
                CornerRadius = new CornerRadius(6)
            };
            TextBlock sepText = new TextBlock
            {
                Text = "🛠️ UI 界面所有元素色彩独立深度调节 (支持任意自定色彩与全光谱选色):",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            sepHeader.Child = sepText;
            p.Children.Add(sepHeader);

            Action onColorChanged = () =>
            {
                ApplyThemeToAllControls();
                _config.Save();
            };

            // 2-column layout grid for the 11 items
            UniformGrid gridColors = new UniformGrid { Columns = 2, Margin = new Thickness(-4, 0, -4, 8) };

            gridColors.Children.Add(CreateUiColorRow("1. 软件主窗口背景", () => _config.UiBgColorHex, val => _config.UiBgColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("2. 卡片面板容器底色", () => _config.UiCardColorHex, val => _config.UiCardColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("3. 顶栏与底栏背景", () => _config.UiHeaderColorHex, val => _config.UiHeaderColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("4. 边框与分割线条颜色", () => _config.UiBorderColorHex, val => _config.UiBorderColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("5. 下拉选项框背景 (修复低对比)", () => _config.UiInputBgColorHex, val => _config.UiInputBgColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("6. 下拉框与输入文字 (超高对比)", () => _config.UiInputTextColorHex, val => _config.UiInputTextColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("7. 主题高亮与强调色", () => _config.ThemeAccentHex, val => _config.ThemeAccentHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("8. 主要功能按钮背景", () => _config.UiButtonBgColorHex, val => _config.UiButtonBgColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("9. 危险与删除按钮色", () => _config.UiDangerColorHex, val => _config.UiDangerColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("10. 全局标签与标题字色", () => _config.UiTextColorHex, val => _config.UiTextColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("11. 次要说明文本颜色", () => _config.UiSubTextColorHex, val => _config.UiSubTextColorHex = val, onColorChanged));

            p.Children.Add(gridColors);

            // Bottom action buttons
            StackPanel bottomBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            Button btnResetApple = CreateButton("↺ 重置为苹果原生深色 (Apple Dark)", double.NaN, 28, NeutralBtnColor);
            btnResetApple.Padding = new Thickness(12, 0, 12, 0);
            btnResetApple.Margin = new Thickness(0, 0, 8, 0);
            btnResetApple.Click += (s, e) =>
            {
                ApplyThemePreset("AppleDark");
                _cboThemePresets.SelectedIndex = 0;
            };
            bottomBtns.Children.Add(btnResetApple);

            Button btnOledBlack = CreateButton("⚡ 一键切换超高对比纯黑 (OLED Pure Black)", double.NaN, 28, NeutralBtnColor);
            btnOledBlack.Padding = new Thickness(12, 0, 12, 0);
            btnOledBlack.Margin = new Thickness(0, 0, 8, 0);
            btnOledBlack.Click += (s, e) =>
            {
                ApplyThemePreset("HighContrastBlack");
            };
            bottomBtns.Children.Add(btnOledBlack);

            Button btnSaveColors = CreateThemedActionButton("💾 立即保存所有UI配色", double.NaN, 28);
            btnSaveColors.Padding = new Thickness(16, 0, 16, 0);
            btnSaveColors.Click += (s, e) =>
            {
                _config.Save();
                MessageBox.Show("所有 UI 界面独立配色已成功保存！下次启动自动生效。", "ScreenCrosshair v6.9", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            bottomBtns.Children.Add(btnSaveColors);

            p.Children.Add(bottomBtns);

            return p;
        }

        // ⭐ USER REQUEST: 软件主窗口背景壁纸自定义设置！
        private UIElement BuildWindowWallpaperContent()
        {
            StackPanel p = new StackPanel();

            StackPanel pathRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
            Button btnBrowseWp = CreateButton("📂 选择本地壁纸图片 (PNG/JPG)...", 200, 28, NeutralBtnColor);
            btnBrowseWp.Click += (s, e) =>
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "选择软件背景壁纸";
                dlg.Filter = "图片文件 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有文件 (*.*)|*.*";
                if (dlg.ShowDialog() == true)
                {
                    _config.WindowBgImagePath = dlg.FileName;
                    _txtWindowWallpaperPath.Text = dlg.FileName;
                    UpdateWindowBackground();
                    _config.Save();
                }
            };
            pathRow.Children.Add(btnBrowseWp);

            Button btnClearWp = CreateButton("✕ 清除壁纸，恢复纯黑", 140, 28, NeutralBtnColor);
            btnClearWp.Margin = new Thickness(10, 0, 0, 0);
            btnClearWp.Click += (s, e) =>
            {
                _config.WindowBgImagePath = "";
                _txtWindowWallpaperPath.Text = "(当前未指定外部壁纸，使用默认深色主题)";
                UpdateWindowBackground();
                _config.Save();
            };
            pathRow.Children.Add(btnClearWp);
            p.Children.Add(pathRow);

            _txtWindowWallpaperPath = new TextBlock
            {
                Text = !string.IsNullOrEmpty(_config.WindowBgImagePath) ? _config.WindowBgImagePath : "(当前未指定外部壁纸，使用默认深色主题)",
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 8)
            };
            p.Children.Add(_txtWindowWallpaperPath);

            p.Children.Add(CreateSliderRow("壁纸不透明度 (%):", 10, 90, (_config.WindowBgOpacity > 0 ? _config.WindowBgOpacity * 100.0 : 35), 
                out _sliderWindowWallpaperOpacity, out _txtWindowWallpaperOpacityVal, val =>
                {
                    _config.WindowBgOpacity = val / 100.0;
                    UpdateWindowBackground();
                    _config.Save();
                }));

            return p;
        }

        private void UpdateWindowBackground()
        {
            if (_windowBgImage == null) return;
            if (!string.IsNullOrEmpty(_config.WindowBgImagePath) && File.Exists(_config.WindowBgImagePath))
            {
                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(_config.WindowBgImagePath, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    _windowBgImage.Source = bi;
                    _windowBgImage.Opacity = _config.WindowBgOpacity > 0 ? _config.WindowBgOpacity : 0.70;
                    if (_windowDarkOverlay != null)
                    {
                        _windowDarkOverlay.Visibility = Visibility.Visible;
                        _windowDarkOverlay.Background = new SolidColorBrush(Color.FromArgb(95, 12, 12, 20));
                    }
                }
                catch
                {
                    _windowBgImage.Source = null;
                }
            }
            else
            {
                _windowBgImage.Source = null;
                if (_windowDarkOverlay != null)
                {
                    _windowDarkOverlay.Visibility = Visibility.Collapsed;
                }
            }
            ApplyThemeToAllControls();
        }

        // ⭐ USER REQUEST: 全功能标准调色板方法！
        private void OpenColorPicker(string initialHex, Action<string> onColorChosen)
        {
            try
            {
                using (System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog())
                {
                    cd.FullOpen = true;
                    try
                    {
                        System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(initialHex);
                        cd.Color = c;
                    }
                    catch { }

                    if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string hex = string.Format("#{0:X2}{1:X2}{2:X2}", cd.Color.R, cd.Color.G, cd.Color.B);
                        onColorChosen(hex);
                    }
                }
            }
            catch { }
        }

        private UIElement BuildConfigManagerContent()
        {
            StackPanel p = new StackPanel();
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };

            Button btnExport = CreateButton("导出配置为 JSON 文件...", 180, 28, NeutralBtnColor);
            btnExport.Click += (s, e) =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "导出准星配置文件";
                sfd.Filter = "JSON 文件 (*.json)|*.json";
                sfd.FileName = "CrosshairConfig.json";
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        string jsonPath = AppConfig.GetConfigPath();
                        _config.Save();
                        File.Copy(jsonPath, sfd.FileName, true);
                        MessageBox.Show("配置已成功导出！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            btnRow.Children.Add(btnExport);

            Button btnImport = CreateButton("导入外部 JSON 配置文件...", 180, 28, NeutralBtnColor);
            btnImport.Margin = new Thickness(10, 0, 0, 0);
            btnImport.Click += (s, e) =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "导入准星配置文件";
                ofd.Filter = "JSON 文件 (*.json)|*.json";
                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        string localPath = AppConfig.GetConfigPath();
                        File.WriteAllText(localPath, json);
                        AppConfig loaded = AppConfig.Load();
                        _config = loaded;
                        _selectedLayer = _config.Layers.Count > 0 ? _config.Layers[0] : null;
                        if (_config.Groups.Count > 0) _selectedGroup = _config.Groups[0];
                        RefreshGroupList();
                        RefreshLayerList();
                        LoadSelectedLayerIntoInspector();
                        UpdateWindowBackground();
                        UpdateAllViews();
                        MessageBox.Show("配置导入成功并已生效！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("导入失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            btnRow.Children.Add(btnImport);

            Button btnResetFactory = CreateButton("恢复出厂默认所有配置", 170, 28, DangerBtnColor);
            btnResetFactory.Margin = new Thickness(10, 0, 0, 0);
            btnResetFactory.Click += (s, e) =>
            {
                if (MessageBox.Show("确定要重置所有准星、方案组与设置为默认出厂状态吗？", "确认重置", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _config = AppConfig.CreateDefaultConfig();
                    _selectedLayer = _config.Layers[0];
                    _selectedGroup = _config.Groups[0];
                    _config.Save();
                    RefreshGroupList();
                    RefreshLayerList();
                    LoadSelectedLayerIntoInspector();
                    UpdateWindowBackground();
                    UpdateAllViews();
                    MessageBox.Show("已恢复出厂配置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            btnRow.Children.Add(btnResetFactory);

            p.Children.Add(btnRow);
            return p;
        }

        private UIElement BuildAutoStartContent()
        {
            StackPanel p = new StackPanel();
            _chkStartWithWindows = new CheckBox
            {
                Content = "随 Windows 开机自动启动准星大师 (托盘静默运行)",
                IsChecked = _config.StartWithWindows,
                Foreground = Brushes.White,
                FontSize = 11.5,
                Margin = new Thickness(0, 4, 0, 4)
            };
            _chkStartWithWindows.Checked += (s, e) =>
            {
                _config.StartWithWindows = true;
                SetAutoStartRegistry(true);
                _config.Save();
            };
            _chkStartWithWindows.Unchecked += (s, e) =>
            {
                _config.StartWithWindows = false;
                SetAutoStartRegistry(false);
                _config.Save();
            };
            p.Children.Add(_chkStartWithWindows);
            return p;
        }

        private void SetAutoStartRegistry(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            key.SetValue("CrosshairMaster", "\"" + exePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("CrosshairMaster", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void RegisterThemedInput(Control input)
        {
            if (!_themedInputs.Contains(input))
            {
                _themedInputs.Add(input);
            }
        }

        private Style CreateThemedComboBoxItemStyle()
        {
            string accentHex = _config.ThemeAccentHex ?? "#00E5FF";
            Color accentColor;
            try { accentColor = (Color)ColorConverter.ConvertFromString(accentHex); }
            catch { accentColor = Color.FromRgb(0, 229, 255); }

            // High contrast colors dynamically calculated from active accent
            Color hoverFill = Color.FromArgb(235,
                (byte)Math.Min(255, 12 + (int)(accentColor.R * 0.35)),
                (byte)Math.Min(255, 25 + (int)(accentColor.G * 0.35)),
                (byte)Math.Min(255, 55 + (int)(accentColor.B * 0.35))
            );
            Color selFill = Color.FromArgb(250,
                (byte)Math.Min(255, 16 + (int)(accentColor.R * 0.65)),
                (byte)Math.Min(255, 36 + (int)(accentColor.G * 0.65)),
                (byte)Math.Min(255, 85 + (int)(accentColor.B * 0.65))
            );

            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.SnapsToDevicePixelsProperty, true));
            itemStyle.Setters.Add(new Setter(Control.OverridesDefaultStyleProperty, true));
            itemStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.5));
            itemStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            itemStyle.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));

            ControlTemplate template = new ControlTemplate(typeof(ComboBoxItem));

            // Outer border with subtle rounding and clear dark background
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "Bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            border.SetValue(Border.MarginProperty, new Thickness(4, 2, 4, 2));
            border.SetValue(Border.PaddingProperty, new Thickness(12, 8, 12, 8));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.BorderBrushProperty, _themeBorderBrush);
            border.SetValue(Border.BackgroundProperty, _themeInputBgBrush);

            // Container Grid for left accent indicator + content + right checkmark
            FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid));

            FrameworkElementFactory col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            FrameworkElementFactory col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);

            grid.AppendChild(col0);
            grid.AppendChild(col1);
            grid.AppendChild(col2);

            // Left accent pill (shown when selected)
            FrameworkElementFactory pill = new FrameworkElementFactory(typeof(Border), "SelPill");
            pill.SetValue(Border.WidthProperty, 4.0);
            pill.SetValue(Border.HeightProperty, 16.0);
            pill.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            pill.SetValue(Border.BackgroundProperty, _themeAccentBrush);
            pill.SetValue(Border.MarginProperty, new Thickness(0, 0, 10, 0));
            pill.SetValue(Border.VisibilityProperty, Visibility.Collapsed);
            pill.SetValue(Grid.ColumnProperty, 0);
            grid.AppendChild(pill);

            // Content presenter (with clear legible text)
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter), "CP");
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            content.SetValue(Grid.ColumnProperty, 1);
            grid.AppendChild(content);

            // Right checkmark indicator (shown when selected)
            FrameworkElementFactory check = new FrameworkElementFactory(typeof(TextBlock), "SelCheck");
            check.SetValue(TextBlock.TextProperty, "✔ 当前");
            check.SetValue(TextBlock.FontSizeProperty, 11.0);
            check.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            check.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            check.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            check.SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
            check.SetValue(TextBlock.VisibilityProperty, Visibility.Collapsed);
            check.SetValue(Grid.ColumnProperty, 2);
            grid.AppendChild(check);

            border.AppendChild(grid);
            template.VisualTree = border;

            // 1. Mouse Hover (IsHighlighted) - Vibrant electric highlight fill, glowing theme border
            Trigger trgHover = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            trgHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverFill), "Bd"));
            trgHover.Setters.Add(new Setter(Border.BorderBrushProperty, _themeAccentBrush, "Bd"));
            trgHover.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            trgHover.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));

            // 2. Selected (IsSelected) - Solid high-contrast neon fill, bright border, glowing pill, checkmark
            Trigger trgSel = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            trgSel.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(selFill), "Bd"));
            trgSel.Setters.Add(new Setter(Border.BorderBrushProperty, _themeAccentBrush, "Bd"));
            trgSel.Setters.Add(new Setter(Border.VisibilityProperty, Visibility.Visible, "SelPill"));
            trgSel.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Visible, "SelCheck"));
            trgSel.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            trgSel.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));
            trgSel.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            trgSel.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));

            // 3. Selected AND Hovered
            MultiTrigger trgSelHover = new MultiTrigger();
            trgSelHover.Conditions.Add(new Condition(ComboBoxItem.IsSelectedProperty, true));
            trgSelHover.Conditions.Add(new Condition(ComboBoxItem.IsHighlightedProperty, true));
            trgSelHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, (byte)Math.Min(255, selFill.R + 25), (byte)Math.Min(255, selFill.G + 25), (byte)Math.Min(255, selFill.B + 25))), "Bd"));
            trgSelHover.Setters.Add(new Setter(Border.BorderBrushProperty, _themeAccentBrush, "Bd"));
            trgSelHover.Setters.Add(new Setter(Border.VisibilityProperty, Visibility.Visible, "SelPill"));
            trgSelHover.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Visible, "SelCheck"));
            trgSelHover.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            trgSelHover.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));
            trgSelHover.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            trgSelHover.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));

            // 4. Disabled
            Trigger trgDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            trgDisabled.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(105, 110, 125))));
            trgDisabled.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(105, 110, 125))));

            template.Triggers.Add(trgHover);
            template.Triggers.Add(trgSel);
            template.Triggers.Add(trgSelHover);
            template.Triggers.Add(trgDisabled);

            itemStyle.Setters.Add(new Setter(Control.TemplateProperty, template));
            return itemStyle;
        }

        private Border CreateCardBorder()
        {
            Border b = new Border
            {
                Background = _themeCardBrush,
                BorderBrush = _themeBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };
            _cardBorders.Add(b);
            return b;
        }

        // ⭐ USER REQUEST: 苹果风格高质感拖拽比例分割条 (Apple macOS Style GridSplitter)
        private ControlTemplate CreateSplitterTemplate()
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "splitBd");
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            border.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            // 居中极简细线指示条
            FrameworkElementFactory line = new FrameworkElementFactory(typeof(Border), "splitLine");
            line.SetValue(Border.WidthProperty, 2.0);
            line.SetValue(Border.CornerRadiusProperty, new CornerRadius(1.0));
            line.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            line.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            line.SetValue(Border.MarginProperty, new Thickness(0, 16, 0, 16));
            line.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)));

            border.AppendChild(line);

            ControlTemplate template = new ControlTemplate(typeof(GridSplitter));
            template.VisualTree = border;

            // 悬停交互高亮
            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, _themeAccentBrush, "splitLine"));
            hoverTrigger.Setters.Add(new Setter(Border.WidthProperty, 4.0, "splitLine"));
            hoverTrigger.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(2.0), "splitLine"));
            template.Triggers.Add(hoverTrigger);

            // 拖拽中交互高亮
            Trigger dragTrigger = new Trigger { Property = UIElement.IsMouseCaptureWithinProperty, Value = true };
            dragTrigger.Setters.Add(new Setter(Border.BackgroundProperty, _themeAccentBrush, "splitLine"));
            dragTrigger.Setters.Add(new Setter(Border.WidthProperty, 4.0, "splitLine"));
            dragTrigger.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(2.0), "splitLine"));
            template.Triggers.Add(dragTrigger);

            return template;
        }

        private GridSplitter CreateThemedSplitter()
        {
            GridSplitter splitter = new GridSplitter
            {
                Width = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                ResizeDirection = GridResizeDirection.Columns,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeWE,
                ShowsPreview = false
            };
            splitter.Template = CreateSplitterTemplate();
            splitter.DragCompleted += (s, e) => SaveColumnRatios();
            return splitter;
        }

        private void SaveColumnRatios()
        {
            if (_colLeft != null && _colCenter != null && _colRight != null)
            {
                if (_colLeft.Width.IsStar && _colCenter.Width.IsStar && _colRight.Width.IsStar)
                {
                    _config.ColumnLeftRatio = _colLeft.Width.Value;
                    _config.ColumnCenterRatio = _colCenter.Width.Value;
                    _config.ColumnRightRatio = _colRight.Width.Value;
                    _config.Save();
                }
            }
        }

        private Border CreateSettingsCard(string title, string description, UIElement content)
        {
            Border card = CreateCardBorder();
            card.Padding = new Thickness(15);
            card.Margin = new Thickness(0, 0, 0, 15);

            StackPanel cardStack = new StackPanel();

            TextBlock t = new TextBlock
            {
                Text = title,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = _themeAccentBrush
            };
            _sectionHeaders.Add(t);

            TextBlock d = new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 3, 0, 10)
            };
            cardStack.Children.Add(t);
            cardStack.Children.Add(d);
            cardStack.Children.Add(content);

            card.Child = cardStack;
            return card;
        }

        private TextBlock CreateSectionHeader(string text)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = _themeAccentBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Margin = new Thickness(0, 8, 0, 3)
            };
            _sectionHeaders.Add(tb);
            return tb;
        }

        private Button CreateButton(string text, double width, double height, Color bgColor)
        {
            return new Button
            {
                Content = text,
                Width = width,
                Height = height,
                Background = new SolidColorBrush(bgColor),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private Button CreateThemedActionButton(string text, double width, double height)
        {
            return new Button
            {
                Content = text,
                Width = width,
                Height = height,
                Background = _themeAccentBrush,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private Button CreateTabNavButton(string text, bool isActive)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 32,
                Padding = new Thickness(15, 0, 15, 0),
                Margin = new Thickness(0, 0, 8, 0),
                Background = isActive ? _themeAccentBrush : Brushes.Transparent,
                Foreground = isActive ? Brushes.Black : Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            return btn;
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
        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_config.IsPositionLocked)
                {
                    _txtLockStatus.Text = "【提示】当前处于锁定状态！请先点击右上角“🔓坐标已解锁”解除锁定再拖拽。";
                    return;
                }

                if (_selectedLayer != null)
                {
                    _isDraggingInPreview = true;
                    _dragStartPoint = e.GetPosition(_previewCanvas);
                    _dragStartOffsetX = _selectedLayer.OffsetX;
                    _dragStartOffsetY = _selectedLayer.OffsetY;
                    _previewBox.CaptureMouse();
                }
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingInPreview && _selectedLayer != null && !_config.IsPositionLocked)
            {
                Point current = e.GetPosition(_previewCanvas);
                double scale = _config.PreviewZoom > 0 ? _config.PreviewZoom : 10.0;
                double deltaX = (current.X - _dragStartPoint.X) / scale;
                double deltaY = (current.Y - _dragStartPoint.Y) / scale;

                _selectedLayer.OffsetX = Math.Round(_dragStartOffsetX + deltaX, 3);
                _selectedLayer.OffsetY = Math.Round(_dragStartOffsetY + deltaY, 3);

                UpdateInspectorOffsets();
                _overlayWindow.SetLayers(_config.Layers);
            }
        }

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingInPreview)
            {
                _isDraggingInPreview = false;
                _previewBox.ReleaseMouseCapture();
                _config.Save();
            }
        }

        private void TogglePositionLock()
        {
            _config.IsPositionLocked = !_config.IsPositionLocked;
            UpdateLockStatusUI();
            _config.Save();
        }

        private void UpdateLockStatusUI()
        {
            if (_btnLockPosition != null)
            {
                _btnLockPosition.Content = _config.IsPositionLocked ? "🔒 坐标已锁定" : "🔓 坐标已解锁";
                _btnLockPosition.Background = new SolidColorBrush(_config.IsPositionLocked ? DangerBtnColor : NeutralBtnColor);
            }
            if (_txtLockStatus != null)
            {
                _txtLockStatus.Text = _config.IsPositionLocked ? "【已锁定防误触】无法拖动，请先点击右上角解锁" : "【已解锁】可在上方预览框直接拖动，或点击下方按钮在屏幕上直接拖拽";
                _txtLockStatus.Foreground = _config.IsPositionLocked ? new SolidColorBrush(Color.FromRgb(255, 105, 97)) : new SolidColorBrush(Color.FromRgb(142, 142, 147));
            }
        }

        private Button CreateNudgeButton(string text, double delta, bool isX)
        {
            Button btn = CreateButton(text, 27, 22, NeutralBtnColor);
            btn.FontSize = 10;
            btn.Padding = new Thickness(0);
            btn.Margin = new Thickness(1.5, 0, 1.5, 0);
            btn.Click += (s, e) =>
            {
                if (_config.IsPositionLocked)
                {
                    MessageBox.Show("当前坐标已锁定！如需微调请先点击上方【锁定/解锁】按钮。", "ScreenCrosshair", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (_selectedLayer != null)
                {
                    if (isX) _selectedLayer.OffsetX = Math.Round(_selectedLayer.OffsetX + delta, 3);
                    else _selectedLayer.OffsetY = Math.Round(_selectedLayer.OffsetY + delta, 3);
                    UpdateInspectorOffsets();
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                }
            };
            return btn;
        }

        // ======================= ⭐ Keyboard Listener & Hotkey Recording =======================
        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (_isRecordingOverlayKey)
            {
                e.Handled = true;
                _isRecordingOverlayKey = false;
                int vk = KeyInterop.VirtualKeyFromKey(key);
                _config.ToggleHotKeyVK = vk;
                _config.ToggleHotKeyName = key.ToString();
                _btnRecordOverlayKey.Content = string.Format("【{0}】点击更改", _config.ToggleHotKeyName);
                _btnRecordOverlayKey.Background = new SolidColorBrush(NeutralBtnColor);

                if (_hotKeyManager != null)
                {
                    _hotKeyManager.Register(_config.ToggleHotKeyVK, _config.ToggleHotKeyMod, _config.SettingsHotKeyVK, _config.SettingsHotKeyMod);
                }
                UpdateStatusBadge();
                _config.Save();
                return;
            }
            else if (_isRecordingSettingsKey)
            {
                e.Handled = true;
                _isRecordingSettingsKey = false;
                int vk = KeyInterop.VirtualKeyFromKey(key);
                _config.SettingsHotKeyVK = vk;
                _config.SettingsHotKeyName = key.ToString();
                _btnRecordSettingsKey.Content = string.Format("【{0}】点击更改", _config.SettingsHotKeyName);
                _btnRecordSettingsKey.Background = new SolidColorBrush(NeutralBtnColor);

                if (_hotKeyManager != null)
                {
                    _hotKeyManager.Register(_config.ToggleHotKeyVK, _config.ToggleHotKeyMod, _config.SettingsHotKeyVK, _config.SettingsHotKeyMod);
                }
                UpdateStatusBadge();
                _config.Save();
                return;
            }

            if (!_config.IsPositionLocked && _selectedLayer != null)
            {
                double step = 1.0;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) step = 5.0;
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) step = 0.1;
                else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) step = 0.001;

                if (key == Key.Left)
                {
                    _selectedLayer.OffsetX = Math.Round(_selectedLayer.OffsetX - step, 3);
                    UpdateInspectorOffsets();
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                    e.Handled = true;
                }
                else if (key == Key.Right)
                {
                    _selectedLayer.OffsetX = Math.Round(_selectedLayer.OffsetX + step, 3);
                    UpdateInspectorOffsets();
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                    e.Handled = true;
                }
                else if (key == Key.Up)
                {
                    _selectedLayer.OffsetY = Math.Round(_selectedLayer.OffsetY - step, 3);
                    UpdateInspectorOffsets();
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                    e.Handled = true;
                }
                else if (key == Key.Down)
                {
                    _selectedLayer.OffsetY = Math.Round(_selectedLayer.OffsetY + step, 3);
                    UpdateInspectorOffsets();
                    _overlayWindow.SetLayers(_config.Layers);
                    _config.Save();
                    e.Handled = true;
                }
            }
        }

        // ======================= ⭐ Groups System =======================
        private void RefreshGroupList()
        {
            _isUpdatingUi = true;
            _cboGroups.Items.Clear();

            int selectedIndex = 0;
            for (int i = 0; i < _config.Groups.Count; i++)
            {
                CrosshairGroup g = _config.Groups[i];
                _cboGroups.Items.Add(string.Format("{0} ({1}个准星)", g.Name, g.Layers.Count));
                if (_selectedGroup != null && g.Id == _selectedGroup.Id)
                {
                    selectedIndex = i;
                }
            }

            if (_cboGroups.Items.Count > 0)
            {
                _cboGroups.SelectedIndex = selectedIndex;
                if (_selectedGroup == null) _selectedGroup = _config.Groups[selectedIndex];
            }

            _isUpdatingUi = false;
            RefreshGroupLayersDisplay();
        }

        private void OnGroupSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isUpdatingUi && _cboGroups.SelectedIndex >= 0 && _cboGroups.SelectedIndex < _config.Groups.Count)
            {
                _selectedGroup = _config.Groups[_cboGroups.SelectedIndex];
                _config.SelectedGroupId = _selectedGroup.Id;
                RefreshGroupLayersDisplay();
            }
        }

        private void RefreshGroupLayersDisplay()
        {
            _groupLayersPanel.Children.Clear();
            if (_selectedGroup == null)
            {
                TextBlock emptyTip = new TextBlock
                {
                    Text = "(暂无方案组，点击上方[+新建组]创建)",
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 160)),
                    FontSize = 10.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 15, 0, 15)
                };
                _groupLayersPanel.Children.Add(emptyTip);
                return;
            }

            for (int i = 0; i < _selectedGroup.Layers.Count; i++)
            {
                CrosshairLayer layer = _selectedGroup.Layers[i];
                Border b = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(36, 36, 40)),
                    BorderBrush = _themeBorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                TextBlock name = new TextBlock
                {
                    Text = layer.Name,
                    Foreground = Brushes.White,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand,
                    ToolTip = "双击可重命名此准星"
                };
                name.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        RenameGroupLayer(layer);
                    }
                };

                TextBlock mode = new TextBlock { Text = string.Format("[{0}]", GetModeDisplay(layer.LineMode)), Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)), FontSize = 9.5 };
                info.Children.Add(name);
                info.Children.Add(mode);
                Grid.SetColumn(info, 0);

                StackPanel btns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                Button btnRenameInGroup = CreateMiniButton("✎改名", "自定义方案组内此准星名称", NeutralBtnColor);
                btnRenameInGroup.Width = 46;
                btnRenameInGroup.Click += (s, e) =>
                {
                    RenameGroupLayer(layer);
                };

                Button btnImportToScreen = CreateMiniButton("+载入", "将此准星追加载入到当前屏幕", NeutralBtnColor);
                btnImportToScreen.Width = 44;
                btnImportToScreen.Click += (s, e) =>
                {
                    CrosshairLayer imported = layer.Clone();
                    imported.Enabled = true;
                    _config.Layers.Add(imported);
                    _selectedLayer = imported;
                    RefreshLayerList();
                    LoadSelectedLayerIntoInspector();
                    UpdateAllViews();
                    _config.Save();
                };

                Button btnDelFromGroup = CreateMiniButton("✕", "从组中删除", DangerBtnColor);
                btnDelFromGroup.Click += (s, e) =>
                {
                    _selectedGroup.Layers.Remove(layer);
                    RefreshGroupList();
                    _config.Save();
                };

                btns.Children.Add(btnRenameInGroup);
                btns.Children.Add(btnImportToScreen);
                btns.Children.Add(btnDelFromGroup);
                Grid.SetColumn(btns, 1);

                g.Children.Add(info);
                g.Children.Add(btns);
                b.Child = g;

                _groupLayersPanel.Children.Add(b);
            }
        }

        private void RenameGroupLayer(CrosshairLayer layer)
        {
            if (layer == null) return;
            string newName = Microsoft.VisualBasic.Interaction.InputBox("请输入方案组中该准星的新名称:", "自定义准星名称 - ScreenCrosshair v6.9", layer.Name);
            if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newName.Trim()))
            {
                layer.Name = newName.Trim();
                RefreshGroupLayersDisplay();
                _config.Save();
            }
        }

        private void CreateNewGroup()
        {
            CrosshairGroup g = new CrosshairGroup();
            string defName = string.Format("【方案组 #{0}】", _config.Groups.Count + 1);
            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入新建方案组名称:", "新建方案组 - ScreenCrosshair v6.9", defName);
            g.Name = (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(input.Trim())) ? input.Trim() : defName;
            _config.Groups.Add(g);
            _selectedGroup = g;
            RefreshGroupList();
            _config.Save();
        }

        private void RenameCurrentGroup()
        {
            if (_selectedGroup == null) return;
            string newName = Microsoft.VisualBasic.Interaction.InputBox("请输入方案组新名称:", "方案组重命名 - ScreenCrosshair v6.9", _selectedGroup.Name);
            if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newName.Trim()))
            {
                _selectedGroup.Name = newName.Trim();
                RefreshGroupList();
                _config.Save();
            }
        }

        private void DeleteCurrentGroup()
        {
            if (_selectedGroup != null)
            {
                _config.Groups.Remove(_selectedGroup);
                _selectedGroup = _config.Groups.Count > 0 ? _config.Groups[0] : null;
                RefreshGroupList();
                _config.Save();
            }
        }

        private void ApplyGroupToScreen()
        {
            if (_selectedGroup == null || _selectedGroup.Layers.Count == 0)
            {
                MessageBox.Show("当前方案组中暂无准星图层！", "ScreenCrosshair", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _config.Layers.Clear();
            foreach (CrosshairLayer l in _selectedGroup.Layers)
            {
                _config.Layers.Add(l.Clone());
            }

            _selectedLayer = _config.Layers.Count > 0 ? _config.Layers[0] : null;
            RefreshLayerList();
            LoadSelectedLayerIntoInspector();
            UpdateAllViews();
            _config.Save();
        }

        private void SaveScreenLayersToCurrentGroup()
        {
            if (_selectedGroup == null)
            {
                CreateNewGroup();
            }
            if (_selectedGroup == null) return;
            _selectedGroup.Layers.Clear();
            foreach (CrosshairLayer l in _config.Layers)
            {
                _selectedGroup.Layers.Add(l.Clone());
            }
            RefreshGroupList();
            _config.Save();
            MessageBox.Show(string.Format("已将屏幕上的 {0} 个准星图层全部成功存入【{1}】！", _selectedGroup.Layers.Count, _selectedGroup.Name), "ScreenCrosshair", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ======================= Layer Inspector & Line Mode Fix =======================
        private void OnLineModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isUpdatingUi && _selectedLayer != null)
            {
                switch (_cboMode.SelectedIndex)
                {
                    case 0: _selectedLayer.LineMode = "SolidHorizontal"; break; // ⭐ 贯通实心横线 (无中缝)
                    case 1: _selectedLayer.LineMode = "SolidVertical"; break;   // ⭐ 贯通实心竖线 (无中缝)
                    case 2: _selectedLayer.LineMode = "SolidCross"; break;      // ⭐ 贯通实心十字 (无中缝)
                    case 3: _selectedLayer.LineMode = "Both"; break;
                    case 4: _selectedLayer.LineMode = "HorizontalOnly"; break;
                    case 5: _selectedLayer.LineMode = "VerticalOnly"; break;
                    case 6: _selectedLayer.LineMode = "XCross"; break;          // ⭐ 斜十字
                    case 7: _selectedLayer.LineMode = "Chevron"; break;         // ⭐ 三角箭标
                    case 8: _selectedLayer.LineMode = "TShape"; break;
                    case 9: 
                        _selectedLayer.LineMode = "DotOnly";
                        _selectedLayer.ShowDot = true;
                        if (_selectedLayer.DotSize <= 0) _selectedLayer.DotSize = 2;
                        break;
                    case 10: 
                        _selectedLayer.LineMode = "CircleOnly";
                        _selectedLayer.ShowCircle = true;
                        if (_selectedLayer.CircleRadius <= 0) _selectedLayer.CircleRadius = 20;
                        if (_selectedLayer.CircleThickness <= 0) _selectedLayer.CircleThickness = 2;
                        break;
                    case 11: _selectedLayer.LineMode = "Brackets"; break;
                    case 12: _selectedLayer.LineMode = "CustomImage"; break;     // ⭐ 自定义图片准星
                }
                LoadSelectedLayerIntoInspector();
                RefreshLayerList();
                UpdateAllViews();
            }
        }

        private string GetModeDisplay(string mode)
        {
            if (mode == "SolidHorizontal") return "贯通实心横线 ⭐";
            if (mode == "SolidVertical") return "贯通实心竖线 ⭐";
            if (mode == "SolidCross") return "贯通实心十字 ⭐";
            if (mode == "HorizontalOnly") return "分段仅横线";
            if (mode == "VerticalOnly") return "分段仅竖线";
            if (mode == "XCross") return "斜十字 ⭐";
            if (mode == "Chevron") return "三角箭标 ⭐";
            if (mode == "TShape") return "T字";
            if (mode == "DotOnly") return "中心点 ⭐";
            if (mode == "CircleOnly") return "纯圆圈 ⭐";
            if (mode == "Brackets") return "瞄框";
            if (mode == "CustomImage") return "自定义图片准星 ⭐";
            return "全十字";
        }

        public void RefreshLayerList()
        {
            _layerListPanel.Children.Clear();

            for (int i = 0; i < _config.Layers.Count; i++)
            {
                CrosshairLayer layer = _config.Layers[i];
                bool isSelected = (_selectedLayer != null && layer.Id == _selectedLayer.Id);

                Border itemBorder = new Border
                {
                    Background = isSelected ? new SolidColorBrush(Color.FromArgb(50, 10, 132, 255)) : new SolidColorBrush(Color.FromRgb(36, 36, 40)),
                    BorderBrush = isSelected ? _themeAccentBrush : _themeBorderBrush,
                    BorderThickness = new Thickness(isSelected ? 1.5 : 1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };

                Grid rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                CheckBox chkEnable = new CheckBox
                {
                    IsChecked = layer.Enabled,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                chkEnable.Checked += (s, e) => { layer.Enabled = true; UpdateAllViews(); };
                chkEnable.Unchecked += (s, e) => { layer.Enabled = false; UpdateAllViews(); };
                Grid.SetColumn(chkEnable, 0);

                StackPanel infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                TextBlock nameText = new TextBlock
                {
                    Text = layer.Name,
                    FontWeight = FontWeights.Bold,
                    Foreground = isSelected ? _themeAccentBrush : Brushes.White,
                    FontSize = 11.5
                };

                TextBlock modeText = new TextBlock
                {
                    Text = string.Format("[{0}]", GetModeDisplay(layer.LineMode)),
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                    FontSize = 10,
                    Margin = new Thickness(0, 1, 0, 0)
                };
                infoPanel.Children.Add(nameText);
                infoPanel.Children.Add(modeText);
                Grid.SetColumn(infoPanel, 1);

                StackPanel actionPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                int currentIndex = i;

                Button btnSaveToGroup = CreateMiniButton("→存入组", "将此准星存入当前方案组", NeutralBtnColor);
                btnSaveToGroup.Width = 52;
                btnSaveToGroup.Click += (s, e) =>
                {
                    if (_selectedGroup == null)
                    {
                        CreateNewGroup();
                    }
                    if (_selectedGroup != null)
                    {
                        string customName = Microsoft.VisualBasic.Interaction.InputBox(
                            string.Format("请输入保存至方案组【{0}】的准星名称:", _selectedGroup.Name),
                            "存入方案组 - 自定义名称 (v6.9)",
                            layer.Name);
                        if (string.IsNullOrEmpty(customName) || string.IsNullOrEmpty(customName.Trim()))
                        {
                            return;
                        }

                        CrosshairLayer savedLayer = layer.Clone();
                        savedLayer.Name = customName.Trim();
                        _selectedGroup.Layers.Add(savedLayer);
                        RefreshGroupList();
                        _config.Save();
                        MessageBox.Show(string.Format("已将【{0}】成功存入方案组【{1}】！", savedLayer.Name, _selectedGroup.Name), "ScreenCrosshair v6.9", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                };

                Button btnClone = CreateMiniButton("⎘", "复制图层", NeutralBtnColor);
                btnClone.Click += (s, e) =>
                {
                    CrosshairLayer cloned = layer.Clone();
                    _config.Layers.Insert(currentIndex + 1, cloned);
                    _selectedLayer = cloned;
                    RefreshLayerList();
                    LoadSelectedLayerIntoInspector();
                    UpdateAllViews();
                };

                Button btnDelete = CreateMiniButton("✕", "删除图层", DangerBtnColor);
                btnDelete.Click += (s, e) =>
                {
                    if (_config.Layers.Count <= 1)
                    {
                        MessageBox.Show("屏幕上至少保留一个图层！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    _config.Layers.Remove(layer);
                    if (_selectedLayer == layer)
                    {
                        _selectedLayer = _config.Layers[0];
                    }
                    RefreshLayerList();
                    LoadSelectedLayerIntoInspector();
                    UpdateAllViews();
                };

                actionPanel.Children.Add(btnSaveToGroup);
                actionPanel.Children.Add(btnClone);
                actionPanel.Children.Add(btnDelete);
                Grid.SetColumn(actionPanel, 2);

                rowGrid.Children.Add(chkEnable);
                rowGrid.Children.Add(infoPanel);
                rowGrid.Children.Add(actionPanel);
                itemBorder.Child = rowGrid;

                CrosshairLayer targetLayer = layer;
                itemBorder.MouseDown += (s, e) =>
                {
                    _selectedLayer = targetLayer;
                    _config.SelectedLayerId = targetLayer.Id;
                    RefreshLayerList();
                    LoadSelectedLayerIntoInspector();
                    UpdatePreview();
                };

                _layerListPanel.Children.Add(itemBorder);
            }
        }

        private void LoadSelectedLayerIntoInspector()
        {
            if (_selectedLayer == null) return;
            _isUpdatingUi = true;

            _txtName.Text = _selectedLayer.Name ?? "";

            string m = _selectedLayer.LineMode ?? "Both";
            if (m == "SolidHorizontal") _cboMode.SelectedIndex = 0;
            else if (m == "SolidVertical") _cboMode.SelectedIndex = 1;
            else if (m == "SolidCross") _cboMode.SelectedIndex = 2;
            else if (m == "Both") _cboMode.SelectedIndex = 3;
            else if (m == "HorizontalOnly") _cboMode.SelectedIndex = 4;
            else if (m == "VerticalOnly") _cboMode.SelectedIndex = 5;
            else if (m == "XCross") _cboMode.SelectedIndex = 6;
            else if (m == "Chevron") _cboMode.SelectedIndex = 7;
            else if (m == "TShape") _cboMode.SelectedIndex = 8;
            else if (m == "DotOnly") _cboMode.SelectedIndex = 9;
            else if (m == "CircleOnly") _cboMode.SelectedIndex = 10;
            else if (m == "Brackets") _cboMode.SelectedIndex = 11;
            else if (m == "CustomImage") _cboMode.SelectedIndex = 12;
            else _cboMode.SelectedIndex = 3;

            // Custom Image Panel toggle
            if (m == "CustomImage" && _customImageBox != null)
            {
                _customImageBox.Visibility = Visibility.Visible;
                _txtCustomImagePath.Text = !string.IsNullOrEmpty(_selectedLayer.CustomImagePath) ? _selectedLayer.CustomImagePath : "(暂未选择图片文件)";
                if (_chkLockAspectRatio != null)
                {
                    _chkLockAspectRatio.IsChecked = _selectedLayer.LockAspectRatio;
                }
                _sliderImageW.Value = _selectedLayer.ImageWidth > 0 ? _selectedLayer.ImageWidth : 32;
                _txtImageWVal.Text = _sliderImageW.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                _sliderImageH.Value = _selectedLayer.ImageHeight > 0 ? _selectedLayer.ImageHeight : 32;
                _txtImageHVal.Text = _sliderImageH.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                _sliderImageRot.Value = _selectedLayer.ImageRotation;
                _txtImageRotVal.Text = _sliderImageRot.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (_customImageBox != null)
            {
                _customImageBox.Visibility = Visibility.Collapsed;
            }

            _sliderLength.Value = _selectedLayer.Length;
            _txtLengthVal.Text = _selectedLayer.Length.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            _sliderThickness.Value = _selectedLayer.Thickness;
            _txtThicknessVal.Text = _selectedLayer.Thickness.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            _sliderGap.Value = _selectedLayer.Gap;
            _txtGapVal.Text = _selectedLayer.Gap.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            if (_sliderRotation != null)
            {
                _sliderRotation.Value = _selectedLayer.Rotation;
                _txtRotationVal.Text = _selectedLayer.Rotation.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            }

            _sliderAlpha.Value = _selectedLayer.Alpha * 100.0;
            _txtAlphaVal.Text = (_selectedLayer.Alpha * 100.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            _txtColorHex.Text = _selectedLayer.ColorHex ?? "#00FF00";

            _chkOutline.IsChecked = _selectedLayer.ShowOutline;
            _sliderOutline.Value = _selectedLayer.OutlineThickness;
            _txtOutlineVal.Text = _selectedLayer.OutlineThickness.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            _chkDot.IsChecked = _selectedLayer.ShowDot || (_selectedLayer.LineMode == "DotOnly");
            _cboDotShape.SelectedIndex = (_selectedLayer.DotShape == "Circle" ? 1 : 0);
            _sliderDotSize.Value = _selectedLayer.DotSize;
            _txtDotSizeVal.Text = _selectedLayer.DotSize.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            _txtDotColor.Text = _selectedLayer.DotColorHex ?? "#FF0000";
            _chkDotOutline.IsChecked = _selectedLayer.DotOutline;

            _chkCircle.IsChecked = _selectedLayer.ShowCircle || (_selectedLayer.LineMode == "CircleOnly");
            _sliderCircleRadius.Value = _selectedLayer.CircleRadius > 0 ? _selectedLayer.CircleRadius : 20;
            _txtCircleRadiusVal.Text = _sliderCircleRadius.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            _sliderCircleThick.Value = _selectedLayer.CircleThickness > 0 ? _selectedLayer.CircleThickness : 2;
            _txtCircleThickVal.Text = _sliderCircleThick.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            _txtCircleColor.Text = _selectedLayer.CircleColorHex ?? "#00FF00";

            if (_chkLayerHideOnLeftMouse != null)
            {
                _chkLayerHideOnLeftMouse.IsChecked = _selectedLayer.HideOnLeftMouse;
            }
            if (_chkLayerHideOnRightMouse != null)
            {
                _chkLayerHideOnRightMouse.IsChecked = _selectedLayer.HideOnRightMouse;
            }

            UpdateInspectorOffsets();

            _isUpdatingUi = false;
        }

        private double GetImageAspectRatio(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 1.0;
            try
            {
                BitmapImage bi = CrosshairRenderer.GetCachedBitmap(path);
                if (bi != null && bi.PixelHeight > 0)
                {
                    return (double)bi.PixelWidth / bi.PixelHeight;
                }
            }
            catch { }
            return 1.0;
        }

        private void UpdateInspectorOffsets()
        {
            if (_selectedLayer != null)
            {
                _isUpdatingUi = true;
                if (_txtOffsetX != null && !_txtOffsetX.IsKeyboardFocusWithin)
                {
                    _txtOffsetX.Text = _selectedLayer.OffsetX.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                }
                if (_txtOffsetY != null && !_txtOffsetY.IsKeyboardFocusWithin)
                {
                    _txtOffsetY.Text = _selectedLayer.OffsetY.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                }
                _isUpdatingUi = false;
            }
        }

        public void AddNewLayer()
        {
            CrosshairLayer newLayer = new CrosshairLayer();
            newLayer.Name = string.Format("图层 #{0}", _config.Layers.Count + 1);
            _config.Layers.Add(newLayer);
            _selectedLayer = newLayer;

            RefreshLayerList();
            LoadSelectedLayerIntoInspector();
            UpdateAllViews();
        }

        private void UpdatePreview()
        {
            if (_previewCanvas == null) return;
            double scale = _config.PreviewZoom > 0 ? _config.PreviewZoom : 10.0;
            double cx = _previewCanvas.Width / 2.0;
            double cy = _previewCanvas.Height / 2.0;
            bool showGrid = _config.ShowPixelGrid;

            if (_selectedLayer == null && _config.Layers != null && _config.Layers.Count > 0)
            {
                _selectedLayer = _config.Layers.Find(l => l.Id == _config.SelectedLayerId) ?? _config.Layers[0];
            }

            // ⭐ USER REQUEST: 切换仅选中的准星或者全部准星，默认是仅选中的准星
            if (_config.PreviewOnlySelected)
            {
                _previewCanvas.Children.Clear();
                if (showGrid && scale >= 4.0)
                {
                    CrosshairRenderer.DrawPixelGrid(_previewCanvas, cx, cy, scale);
                }
                if (_selectedLayer != null)
                {
                    // ⭐ 预览窗口始终锁定居中显示 (applyOffset = false)，避免拖拽坐标时飞出预览框无法观察
                    CrosshairRenderer.RenderSingleLayer(_previewCanvas, _selectedLayer, cx, cy, scale, applyOffset: false);
                }
                if (showGrid && scale >= 6.0)
                {
                    CrosshairRenderer.DrawCenterMarker(_previewCanvas, cx, cy, scale);
                }
            }
            else
            {
                // ⭐ 预览窗口始终锁定居中显示 (applyOffset = false)，避免拖拽坐标时飞出预览框无法观察
                CrosshairRenderer.RenderLayers(_previewCanvas, _config.Layers, cx, cy, scale, showGrid, applyOffset: false);
            }
        }

        public void UpdateAllViews()
        {
            UpdatePreview();
            _overlayWindow.SetLayers(_config.Layers);
        }

        private void UpdatePreviewBackground()
        {
            if (_previewBox == null) return;

            string bg = _config.PreviewBackground ?? "Dark";

            if (bg == "Checkerboard")
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(24, 24, 34)), null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
                dg.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(36, 36, 48)), null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
                dg.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(36, 36, 48)), null, new RectangleGeometry(new Rect(8, 8, 8, 8))));
                DrawingBrush db = new DrawingBrush(dg) { Viewport = new Rect(0, 0, 16, 16), ViewportUnits = BrushMappingMode.Absolute, TileMode = TileMode.Tile };
                _previewBox.Background = db;
            }
            else if (bg == "Desert")
            {
                LinearGradientBrush lgb = new LinearGradientBrush(Color.FromRgb(95, 84, 68), Color.FromRgb(70, 62, 48), new Point(0, 0), new Point(1, 1));
                _previewBox.Background = lgb;
            }
            else if (bg == "BrightSky")
            {
                LinearGradientBrush lgb = new LinearGradientBrush(Color.FromRgb(180, 205, 230), Color.FromRgb(140, 175, 210), new Point(0, 0), new Point(0, 1));
                _previewBox.Background = lgb;
            }
            else if (bg == "DarkCorner")
            {
                RadialGradientBrush rgb = new RadialGradientBrush(Color.FromRgb(22, 22, 30), Color.FromRgb(8, 8, 12));
                _previewBox.Background = rgb;
            }
            else if (bg == "CustomImage" && !string.IsNullOrEmpty(_config.CustomBgImagePath) && File.Exists(_config.CustomBgImagePath))
            {
                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(_config.CustomBgImagePath, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    ImageBrush ib = new ImageBrush(bi) { Stretch = Stretch.UniformToFill };
                    _previewBox.Background = ib;
                }
                catch
                {
                    _previewBox.Background = new SolidColorBrush(Color.FromRgb(14, 14, 18));
                }
            }
            else
            {
                _previewBox.Background = new SolidColorBrush(Color.FromRgb(14, 14, 18));
            }
        }

        private void OnPreviewBgChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (_cboPreviewBg.SelectedIndex)
            {
                case 0: _config.PreviewBackground = "Dark"; break;
                case 1: _config.PreviewBackground = "Checkerboard"; break;
                case 2: _config.PreviewBackground = "Desert"; break;
                case 3: _config.PreviewBackground = "BrightSky"; break;
                case 4: _config.PreviewBackground = "DarkCorner"; break;
                case 5:
                    if (string.IsNullOrEmpty(_config.CustomBgImagePath) || !File.Exists(_config.CustomBgImagePath))
                    {
                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Title = "选择游戏背景截图";
                        dlg.Filter = "图片文件 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有文件 (*.*)|*.*";
                        if (dlg.ShowDialog() == true)
                        {
                            _config.CustomBgImagePath = dlg.FileName;
                            _config.PreviewBackground = "CustomImage";
                        }
                        else
                        {
                            _cboPreviewBg.SelectedIndex = 0;
                            _config.PreviewBackground = "Dark";
                        }
                    }
                    else
                    {
                        _config.PreviewBackground = "CustomImage";
                    }
                    break;
            }

            UpdatePreviewBackground();
            _config.Save();
        }

        private int GetBgComboIndex(string bg)
        {
            if (bg == "Checkerboard") return 1;
            if (bg == "Desert") return 2;
            if (bg == "BrightSky") return 3;
            if (bg == "DarkCorner") return 4;
            if (bg == "CustomImage") return 5;
            return 0;
        }

        private int GetThemePresetIndex(string theme)
        {
            if (theme == "AppleDark" || string.IsNullOrEmpty(theme)) return 0;
            if (theme == "AppleMint") return 1;
            if (theme == "AppleOrange") return 2;
            if (theme == "ApplePurple") return 3;
            if (theme == "AppleSilver") return 4;
            if (theme == "CyberNeon") return 5;
            if (theme == "CrimsonFury") return 6;
            if (theme == "EsportsEmerald") return 7;
            if (theme == "GoldenVanguard") return 8;
            if (theme == "ApplePink") return 9;
            return 0;
        }

        private void UpdateZoomPillsStyles(StackPanel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                Button b = child as Button;
                if (b != null)
                {
                    bool isCur = b.Content.ToString() == string.Format("{0}x", _config.PreviewZoom);
                    b.Background = isCur ? _themeAccentBrush : new SolidColorBrush(NeutralBtnColor);
                    b.Foreground = isCur ? Brushes.Black : Brushes.White;
                }
            }
        }

        // ⭐ USER REQUEST: 准星物理像素实时预览切换按钮样式更新 (仅选中 vs 全部)
        private void UpdatePreviewSwitchStyles()
        {
            if (_btnPreviewSingle == null || _btnPreviewAll == null) return;
            bool onlySelected = _config.PreviewOnlySelected;

            if (onlySelected)
            {
                _btnPreviewSingle.Background = _themeAccentBrush;
                _btnPreviewSingle.Foreground = Brushes.Black;
                _btnPreviewSingle.FontWeight = FontWeights.Bold;

                _btnPreviewAll.Background = Brushes.Transparent;
                _btnPreviewAll.Foreground = _themeTextBrush ?? Brushes.White;
                _btnPreviewAll.FontWeight = FontWeights.Normal;
            }
            else
            {
                _btnPreviewSingle.Background = Brushes.Transparent;
                _btnPreviewSingle.Foreground = _themeTextBrush ?? Brushes.White;
                _btnPreviewSingle.FontWeight = FontWeights.Normal;

                _btnPreviewAll.Background = _themeAccentBrush;
                _btnPreviewAll.Foreground = Brushes.Black;
                _btnPreviewAll.FontWeight = FontWeights.Bold;
            }
        }

        private void UpdateStatusBadge()
        {
            if (_statusBadge != null)
            {
                _statusBadge.Text = _config.OverlayEnabled ? string.Format("● 准星运行中 ({0})", _config.ToggleHotKeyName) : string.Format("○ 准星已暂停 ({0})", _config.ToggleHotKeyName);
                _statusBadge.Foreground = _config.OverlayEnabled ? new SolidColorBrush(Color.FromRgb(52, 199, 89)) : new SolidColorBrush(Color.FromRgb(142, 142, 147));
            }
            if (_btnToggleOverlay != null)
            {
                _btnToggleOverlay.Content = _config.OverlayEnabled ? string.Format("关闭准星 ({0})", _config.ToggleHotKeyName) : string.Format("开启准星 ({0})", _config.ToggleHotKeyName);
                _btnToggleOverlay.Background = new SolidColorBrush(_config.OverlayEnabled ? DangerBtnColor : NeutralBtnColor);
            }
            if (_txtHkOverlayHint != null)
            {
                _txtHkOverlayHint.Text = string.Format("【{0}】游戏内一键显隐准星 | 【方向键】微调坐标", _config.ToggleHotKeyName);
            }
            if (_txtHkSettingsHint != null)
            {
                _txtHkSettingsHint.Text = string.Format("【{0}】游戏内一键呼出/隐藏设置面板 (在设置页可自定义按键)", _config.SettingsHotKeyName);
            }
        }

        public void ToggleOverlay()
        {
            _config.OverlayEnabled = !_config.OverlayEnabled;
            _overlayWindow.SetOverlayActive(_config.OverlayEnabled);
            UpdateStatusBadge();
            _config.Save();
        }

        private Button CreatePaletteButton(string hex)
        {
            Button btn = new Button
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 5, 0),
                Background = CrosshairRenderer.ParseBrush(hex, 1.0),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };
            btn.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.ColorHex = hex;
                    _txtColorHex.Text = hex;
                    UpdateAllViews();
                }
            };
            return btn;
        }

        private Button CreateDotColorButton(string hex)
        {
            Button btn = new Button
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 4, 0),
                Background = CrosshairRenderer.ParseBrush(hex, 1.0),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };
            btn.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.DotColorHex = hex;
                    _txtDotColor.Text = hex;
                    UpdateAllViews();
                }
            };
            return btn;
        }

        private Button CreateCircleColorButton(string hex)
        {
            Button btn = new Button
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 4, 0),
                Background = CrosshairRenderer.ParseBrush(hex, 1.0),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };
            btn.Click += (s, e) =>
            {
                if (_selectedLayer != null)
                {
                    _selectedLayer.CircleColorHex = hex;
                    _txtCircleColor.Text = hex;
                    UpdateAllViews();
                }
            };
            return btn;
        }

        private Button CreateMiniButton(string text, string tooltip, Color bg)
        {
            return new Button
            {
                Content = text,
                Width = 24,
                Height = 22,
                Margin = new Thickness(2, 0, 0, 0),
                Background = new SolidColorBrush(bg),
                Foreground = Brushes.White,
                FontSize = 10,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip
            };
        }

        private Grid CreateSliderRow(string label, double min, double max, double defVal, out Slider slider, out TextBox txtVal, Action<double> onChanged, bool isDecimal3 = true)
        {
            Grid g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });

            TextBlock lbl = new TextBlock { Text = label, Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = defVal,
                SmallChange = isDecimal3 ? 0.1 : 1.0,
                LargeChange = isDecimal3 ? 1.0 : 5.0,
                IsSnapToTickEnabled = false,
                VerticalAlignment = VerticalAlignment.Center
            };

            txtVal = new TextBox
            {
                Text = isDecimal3 ? defVal.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) : ((int)defVal).ToString(),
                Foreground = _themeAccentBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Height = 22,
                Width = 62,
                Margin = new Thickness(4, 0, 0, 0),
                ToolTip = "支持直接输入3位小数(按Enter生效)；在输入框上滑动鼠标滚轮可微调(按住Ctrl更精细)"
            };
            RegisterThemedInput(txtVal);

            Slider sRef = slider;
            TextBox tRef = txtVal;

            Action applyTextValue = () =>
            {
                string raw = tRef.Text.Trim().Replace(',', '.');
                double parsed;
                if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                {
                    double finalVal = isDecimal3 ? Math.Round(parsed, 3) : Math.Round(parsed);
                    if (sRef.Minimum > finalVal) sRef.Minimum = finalVal;
                    if (sRef.Maximum < finalVal) sRef.Maximum = finalVal;
                    sRef.Value = finalVal;
                    tRef.Text = isDecimal3 ? finalVal.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) : ((int)finalVal).ToString();
                    if (!_isUpdatingUi)
                    {
                        onChanged(finalVal);
                    }
                }
                else
                {
                    tRef.Text = isDecimal3 ? sRef.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) : ((int)sRef.Value).ToString();
                }
            };

            tRef.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    applyTextValue();
                    tRef.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            };
            tRef.LostFocus += (s, e) =>
            {
                applyTextValue();
            };

            tRef.MouseWheel += (s, e) =>
            {
                double stepVal = isDecimal3 ? 0.1 : 1.0;
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    stepVal = 0.001; // Ctrl 精密步进 0.001
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    stepVal = 1.0; // Shift 粗调步进 1.0
                }
                double nextVal = sRef.Value + (e.Delta > 0 ? stepVal : -stepVal);
                nextVal = isDecimal3 ? Math.Round(nextVal, 3) : Math.Round(nextVal);
                if (sRef.Minimum > nextVal) sRef.Minimum = nextVal;
                if (sRef.Maximum < nextVal) sRef.Maximum = nextVal;
                sRef.Value = nextVal;
                tRef.Text = isDecimal3 ? nextVal.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) : ((int)nextVal).ToString();
                if (!_isUpdatingUi)
                {
                    onChanged(nextVal);
                }
                e.Handled = true;
            };

            slider.ValueChanged += (s, e) =>
            {
                if (!tRef.IsKeyboardFocusWithin)
                {
                    double roundVal = isDecimal3 ? Math.Round(sRef.Value, 3) : Math.Round(sRef.Value);
                    tRef.Text = isDecimal3 ? roundVal.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) : ((int)roundVal).ToString();
                }
                if (!_isUpdatingUi)
                {
                    double finalVal = isDecimal3 ? Math.Round(sRef.Value, 3) : Math.Round(sRef.Value);
                    onChanged(finalVal);
                }
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(slider, 1);
            Grid.SetColumn(txtVal, 2);
            g.Children.Add(lbl);
            g.Children.Add(slider);
            g.Children.Add(txtVal);
            return g;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
