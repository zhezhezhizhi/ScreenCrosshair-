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
                MessageBox.Show("所有 UI 界面独立配色已成功保存！下次启动自动生效。", "ScreenCrosshair v7.1", MessageBoxButton.OK, MessageBoxImage.Information);
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

                        MessageBox.Show("配置导入成功并已保存生效！", "导入成功 - ScreenCrosshair v7.1", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("导入配置文件失败: " + ex.Message, "导入错误 - ScreenCrosshair v7.1", MessageBoxButton.OK, MessageBoxImage.Error);
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

    }
}
