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
                "快捷键设置",
                "支持单键与组合键（如 F8、Alt+F8 等），点击按钮后按下目标按键即可绑定。",
                BuildHotkeySettingsContent()
            ));

            // 2. Custom Theme & All UI Colors (⭐ USER REQUEST: 下拉框高对比度修复与全UI元素独立自定义色彩)
            settingsPanel.Children.Add(CreateSettingsCard(
                "界面主题与配色",
                "提供常用主题预设，并支持自定义各界面元素的背景与文字颜色。",
                BuildThemeSettingsContent()
            ));

            // 3. Custom Software Background Wallpaper (⭐ USER REQUEST: 软件主窗口背景壁纸自定义)
            settingsPanel.Children.Add(CreateSettingsCard(
                "窗口背景壁纸",
                "支持导入本地图片作为软件面板背景，可自由调节透明度。",
                BuildWindowWallpaperContent()
            ));

            // 4. Screen Resolution & Auto Centering (⭐ USER REQUEST: 检查屏幕分辨率与变动时自动居中)
            settingsPanel.Children.Add(CreateSettingsCard(
                "屏幕分辨率与显示器",
                "检测当前显示设备物理分辨率，支持多显示器切换与分辨率变动自动对齐。",
                BuildResolutionSettingsContent()
            ));

            // 5. Config Import & Export
            settingsPanel.Children.Add(CreateSettingsCard(
                "配置导入与导出",
                "将准星图层与方案配置导出为 JSON 文件备份，或导入已有配置文件。",
                BuildConfigManagerContent()
            ));

            // 6. Auto Start
            settingsPanel.Children.Add(CreateSettingsCard(
                "开机自启动",
                "设置开机时是否在系统后台自动启动本工具。",
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
                Content = "屏幕分辨率改变时自动重新对齐中心",
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
            Button btnManualCheck = CreateThemedActionButton("重新检测并对齐中心", double.NaN, 28);
            btnManualCheck.Padding = new Thickness(14, 0, 14, 0);
            btnManualCheck.Click += (s, e) => CheckAndRefreshResolution(true);
            btnRow.Children.Add(btnManualCheck);

            p.Children.Add(btnRow);

            TextBlock desc = new TextBlock
            {
                Text = "说明：在游戏切换拉伸分辨率或插拔显示器时，开启自动对齐可确保准星始终对齐物理屏幕正中心。",
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

            TextBlock lbl1 = new TextBlock { Text = "开关准星快捷键:", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _btnRecordOverlayKey = CreateButton(string.Format("【{0}】点击修改", _config.ToggleHotKeyName), 160, 30, NeutralBtnColor);
            _btnRecordOverlayKey.Click += (s, e) =>
            {
                _isRecordingOverlayKey = true;
                _isRecordingSettingsKey = false;
                _btnRecordOverlayKey.Content = "[请按下目标按键...]";
                _btnRecordOverlayKey.Background = new SolidColorBrush(DangerBtnColor);
            };
            TextBlock tip1 = new TextBlock { Text = "默认按键为 F8，支持任意按键或组合键（如 Alt+F8）", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };
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

            TextBlock lbl2 = new TextBlock { Text = "呼出/隐藏面板快捷键:", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _btnRecordSettingsKey = CreateButton(string.Format("【{0}】点击修改", _config.SettingsHotKeyName), 160, 30, NeutralBtnColor);
            _btnRecordSettingsKey.Click += (s, e) =>
            {
                _isRecordingSettingsKey = true;
                _isRecordingOverlayKey = false;
                _btnRecordSettingsKey.Content = "[请按下目标按键...]";
                _btnRecordSettingsKey.Background = new SolidColorBrush(DangerBtnColor);
            };
            TextBlock tip2 = new TextBlock { Text = "默认按键为 F10", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };
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
                _btnRecordOverlayKey.Content = "【F8】点击修改";
                _btnRecordSettingsKey.Content = "【F10】点击修改";
                UpdateStatusBadge();
                _config.Save();
                MessageBox.Show("快捷键已恢复默认 (F8 / F10)", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            p.Children.Add(btnResetHk);

            return p;
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

            TextBlock lbl1 = new TextBlock { Text = "预设主题风格:", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _cboThemePresets = new ComboBox { Height = 28 };
            _cboThemePresets.Items.Add("深空灰 (Dark)");
            _cboThemePresets.Items.Add("薄荷绿 (Mint)");
            _cboThemePresets.Items.Add("日落橙 (Orange)");
            _cboThemePresets.Items.Add("深夜紫 (Purple)");
            _cboThemePresets.Items.Add("钛金属 (Silver)");
            _cboThemePresets.Items.Add("极光青 (Cyan)");
            _cboThemePresets.Items.Add("赤红 (Crimson)");
            _cboThemePresets.Items.Add("翡翠绿 (Emerald)");
            _cboThemePresets.Items.Add("琥珀金 (Gold)");
            _cboThemePresets.Items.Add("樱花粉 (Pink)");
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
            addPill("深空灰", "AppleDark", Color.FromRgb(44, 44, 46));
            addPill("樱花粉", "ApplePink", Color.FromRgb(225, 95, 145));
            addPill("极光青", "CyberNeon", Color.FromRgb(0, 160, 190));
            addPill("翡翠绿", "EsportsEmerald", Color.FromRgb(0, 150, 70));
            addPill("赤红", "CrimsonFury", Color.FromRgb(190, 40, 40));
            addPill("深夜紫", "ApplePurple", Color.FromRgb(140, 60, 180));
            addPill("日落橙", "AppleOrange", Color.FromRgb(190, 100, 10));
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
                Text = "界面元素配色微调:",
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

            gridColors.Children.Add(CreateUiColorRow("窗口背景", () => _config.UiBgColorHex, val => _config.UiBgColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("卡片面板背景", () => _config.UiCardColorHex, val => _config.UiCardColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("顶栏与底栏", () => _config.UiHeaderColorHex, val => _config.UiHeaderColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("边框线条", () => _config.UiBorderColorHex, val => _config.UiBorderColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("输入框与下拉框背景", () => _config.UiInputBgColorHex, val => _config.UiInputBgColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("输入框文本颜色", () => _config.UiInputTextColorHex, val => _config.UiInputTextColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("强调高亮色", () => _config.ThemeAccentHex, val => _config.ThemeAccentHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("主按钮背景", () => _config.UiButtonBgColorHex, val => _config.UiButtonBgColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("危险/删除按钮背景", () => _config.UiDangerColorHex, val => _config.UiDangerColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("主要文字颜色", () => _config.UiTextColorHex, val => _config.UiTextColorHex = val, onColorChanged));
            gridColors.Children.Add(CreateUiColorRow("次要说明文字", () => _config.UiSubTextColorHex, val => _config.UiSubTextColorHex = val, onColorChanged));

            p.Children.Add(gridColors);

            // Bottom action buttons
            StackPanel bottomBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            Button btnResetApple = CreateButton("重置为默认主题", double.NaN, 28, NeutralBtnColor);
            btnResetApple.Padding = new Thickness(12, 0, 12, 0);
            btnResetApple.Margin = new Thickness(0, 0, 8, 0);
            btnResetApple.Click += (s, e) =>
            {
                ApplyThemePreset("AppleDark");
                _cboThemePresets.SelectedIndex = 0;
            };
            bottomBtns.Children.Add(btnResetApple);

            Button btnOledBlack = CreateButton("纯黑高对比模式", double.NaN, 28, NeutralBtnColor);
            btnOledBlack.Padding = new Thickness(12, 0, 12, 0);
            btnOledBlack.Margin = new Thickness(0, 0, 8, 0);
            btnOledBlack.Click += (s, e) =>
            {
                ApplyThemePreset("HighContrastBlack");
            };
            bottomBtns.Children.Add(btnOledBlack);

            Button btnSaveColors = CreateThemedActionButton("保存配色", double.NaN, 28);
            btnSaveColors.Padding = new Thickness(16, 0, 16, 0);
            btnSaveColors.Click += (s, e) =>
            {
                _config.Save();
                MessageBox.Show("配色方案已保存", "ScreenCrosshair v7.3", MessageBoxButton.OK, MessageBoxImage.Information);
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
                _txtWindowWallpaperPath.Text = "(未设置背景图片，使用主题底色)";
                UpdateWindowBackground();
                _config.Save();
            };
            pathRow.Children.Add(btnClearWp);
            p.Children.Add(pathRow);

            _txtWindowWallpaperPath = new TextBlock
            {
                Text = !string.IsNullOrEmpty(_config.WindowBgImagePath) ? _config.WindowBgImagePath : "(未设置背景图片，使用主题底色)",
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
                        AppConfig loaded = AppConfig.FromJson(json);

                        _config = loaded;
                        _selectedLayer = _config.Layers.Find(l => l.Id == _config.SelectedLayerId) ?? (_config.Layers.Count > 0 ? _config.Layers[0] : null);
                        if (_selectedLayer != null) _config.SelectedLayerId = _selectedLayer.Id;

                        if (_config.Groups.Count > 0)
                        {
                            _selectedGroup = _config.Groups.Find(g => g.Id == _config.SelectedGroupId) ?? _config.Groups[0];
                            _config.SelectedGroupId = _selectedGroup.Id;
                        }
                        else
                        {
                            _selectedGroup = null;
                            _config.SelectedGroupId = "";
                        }

                        // ⭐ USER REQUEST: 导入配置后立即保存到本地持久化文件！
                        _config.Save();

                        // 同步刷新主题与外观笔刷
                        ComputeThemeBrushes();

                        // 同步刷新全屏准星覆盖层
                        if (_overlayWindow != null)
                        {
                            _overlayWindow.AutoRefreshEnabled = _config.AutoRefreshOnResolutionChange;
                            _overlayWindow.SetOverlayActive(_config.OverlayEnabled);
                            _overlayWindow.SetLayers(_config.Layers);
                        }

                        // 同步更新全局快捷键注册
                        if (_hotKeyManager != null)
                        {
                            try
                            {
                                _hotKeyManager.Register(_config.ToggleHotKeyVK, _config.ToggleHotKeyMod, _config.SettingsHotKeyVK, _config.SettingsHotKeyMod);
                            }
                            catch { }
                        }

                        // 刷新方案组、图层列表与属性检查器
                        RefreshGroupList();
                        RefreshLayerList();
                        LoadSelectedLayerIntoInspector();
                        UpdateWindowBackground();
                        UpdatePreviewBackground();
                        UpdateAllViews();
                        UpdateResolutionUI();

                        MessageBox.Show("配置导入成功并已保存生效！", "导入成功 - ScreenCrosshair v7.3", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("导入配置文件失败: " + ex.Message, "导入错误 - ScreenCrosshair v7.3", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    _selectedLayer = (_config.Layers != null && _config.Layers.Count > 0) ? _config.Layers[0] : null;
                    _selectedGroup = (_config.Groups != null && _config.Groups.Count > 0) ? _config.Groups[0] : null;
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
                Content = "开机自动启动 (最小化到托盘)",
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

    }
}
