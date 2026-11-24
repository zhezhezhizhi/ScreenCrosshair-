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
            if (mode == "SolidHorizontal") return "贯通横线";
            if (mode == "SolidVertical") return "贯通竖线";
            if (mode == "SolidCross") return "贯通十字";
            if (mode == "HorizontalOnly") return "仅水平线";
            if (mode == "VerticalOnly") return "仅垂直线";
            if (mode == "XCross") return "斜十字";
            if (mode == "Chevron") return "三角箭标";
            if (mode == "TShape") return "T型";
            if (mode == "DotOnly") return "中心点";
            if (mode == "CircleOnly") return "圆圈";
            if (mode == "Brackets") return "方框";
            if (mode == "CustomImage") return "自定义图片";
            return "标准十字";
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

                Button btnSaveToGroup = CreateMiniButton("存入方案", "保存到当前方案", NeutralBtnColor);
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
                            string.Format("请输入准星在方案「{0}」中的名称:", _selectedGroup.Name),
                            "保存至方案",
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
                        MessageBox.Show(string.Format("已保存至方案「{0}」", _selectedGroup.Name), "ScreenCrosshair v7.2", MessageBoxButton.OK, MessageBoxImage.Information);
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
                _txtCustomImagePath.Text = !string.IsNullOrEmpty(_selectedLayer.CustomImagePath) ? _selectedLayer.CustomImagePath : "(未选择图片)";
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
                _statusBadge.Text = _config.OverlayEnabled ? string.Format("● 准星开启 ({0})", _config.ToggleHotKeyName) : string.Format("○ 准星关闭 ({0})", _config.ToggleHotKeyName);
                _statusBadge.Foreground = _config.OverlayEnabled ? new SolidColorBrush(Color.FromRgb(52, 199, 89)) : new SolidColorBrush(Color.FromRgb(142, 142, 147));
            }
            if (_btnToggleOverlay != null)
            {
                _btnToggleOverlay.Content = _config.OverlayEnabled ? string.Format("关闭准星 ({0})", _config.ToggleHotKeyName) : string.Format("开启准星 ({0})", _config.ToggleHotKeyName);
                _btnToggleOverlay.Background = new SolidColorBrush(_config.OverlayEnabled ? DangerBtnColor : NeutralBtnColor);
            }
            if (_txtHkOverlayHint != null)
            {
                _txtHkOverlayHint.Text = string.Format("快捷键: {0} 开关准星 | 方向键微调坐标", _config.ToggleHotKeyName);
            }
            if (_txtHkSettingsHint != null)
            {
                _txtHkSettingsHint.Text = string.Format("快捷键: {0} 呼出/隐藏面板 (可在设置页修改)", _config.SettingsHotKeyName);
            }
        }

        public void ToggleOverlay()
        {
            _config.OverlayEnabled = !_config.OverlayEnabled;
            _overlayWindow.SetOverlayActive(_config.OverlayEnabled);
            UpdateStatusBadge();
            _config.Save();
        }

    }
}
