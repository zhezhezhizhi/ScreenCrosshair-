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
                Text = "准星图层",
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button btnAddLayer = CreateThemedActionButton("+ 新建图层", 80, 26);
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
                Text = "方案组预设",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = _themeAccentBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            _sectionHeaders.Add(txtGroupTitle);

            StackPanel groupBtns = new StackPanel { Orientation = Orientation.Horizontal };
            Button btnNewGroup = CreateMiniButton("新建", "新建方案组", NeutralBtnColor);
            btnNewGroup.Width = 52;
            btnNewGroup.Click += (s, e) => CreateNewGroup();

            Button btnRenameGroup = CreateMiniButton("改名", "重命名当前方案", NeutralBtnColor);
            btnRenameGroup.Width = 42;
            btnRenameGroup.Click += (s, e) => RenameCurrentGroup();

            Button btnDelGroup = CreateMiniButton("删除", "删除当前方案", DangerBtnColor);
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
            Button btnApplyGroup = CreateThemedActionButton("应用方案到屏幕", 260, 26);
            btnApplyGroup.Click += (s, e) => ApplyGroupToScreen();

            Button btnSaveToGroup = CreateButton("将当前屏幕准星存入该方案", 260, 24, NeutralBtnColor);
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
                Text = "准星预览",
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtPreviewTitle, 0);

            StackPanel bgSelectPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock lblBg = new TextBlock { Text = "背景: ", Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            _cboPreviewBg = new ComboBox { Height = 24, Width = 115, FontSize = 11 };
            _cboPreviewBg.Items.Add("纯黑");
            _cboPreviewBg.Items.Add("棋盘格");
            _cboPreviewBg.Items.Add("灰色场景");
            _cboPreviewBg.Items.Add("高亮背景");
            _cboPreviewBg.Items.Add("暗光场景");
            _cboPreviewBg.Items.Add("自定义图片...");
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
                Text = "预览缩放:",
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            _txtZoomVal = new TextBlock
            {
                Text = string.Format("{0}x", _config.PreviewZoom),
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
                    _txtZoomVal.Text = string.Format("{0}x", curZ);
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
                Content = "显示网格线",
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
                Content = "当前图层",
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
                Content = "全部图层",
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
                Text = "坐标校准",
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
                Text = _config.IsPositionLocked ? "坐标已锁定，点击右侧按钮解锁" : "坐标已解锁：可在上方预览框或屏幕上直接拖动",
                Foreground = _config.IsPositionLocked ? new SolidColorBrush(Color.FromRgb(255, 120, 120)) : new SolidColorBrush(Color.FromRgb(142, 142, 147)),
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 4)
            };
            offsetPanel.Children.Add(_txtLockStatus);

            // ⭐ USER REQUEST: 屏幕直接拖拽按钮！
            Button btnScreenDrag = CreateButton("屏幕拖拽定位", double.NaN, 28, NeutralBtnColor);
            btnScreenDrag.HorizontalAlignment = HorizontalAlignment.Stretch;
            btnScreenDrag.Margin = new Thickness(0, 2, 0, 6);
            btnScreenDrag.ToolTip = "在屏幕上直接拖动准星位置，按 ESC 或 Enter 退出并保存";
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

            Button btnResetOffset = CreateButton("重置坐标到中心 (0, 0)", double.NaN, 24, NeutralBtnColor);
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
                    ? string.Format("屏幕: {0}×{1} | 缩放: {2}% (中心: {3}, {4})",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.DpiPercent, initMetrics.PhysicalCenterX, initMetrics.PhysicalCenterY)
                    : string.Format("屏幕: {0}×{1} (中心: {2}, {3})",
                        initMetrics.PhysicalWidth, initMetrics.PhysicalHeight, initMetrics.PhysicalCenterX, initMetrics.PhysicalCenterY),
                Foreground = _themeSubTextBrush ?? Brushes.White,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button btnRefreshRes = CreateButton("刷新分辨率", double.NaN, 22, NeutralBtnColor);
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
                Text = string.Format("快捷键: {0} 开关准星 | 方向键微调坐标", _config.ToggleHotKeyName),
                Foreground = _themeAccentBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            _txtHkSettingsHint = new TextBlock
            {
                Text = string.Format("快捷键: {0} 呼出/隐藏面板 (可在设置页修改)", _config.SettingsHotKeyName),
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
            _cboMode.Items.Add("贯通横线");
            _cboMode.Items.Add("贯通竖线");
            _cboMode.Items.Add("贯通十字");
            _cboMode.Items.Add("标准十字");
            _cboMode.Items.Add("仅水平线");
            _cboMode.Items.Add("仅垂直线");
            _cboMode.Items.Add("斜十字 (X)");
            _cboMode.Items.Add("三角箭标");
            _cboMode.Items.Add("T型准星");
            _cboMode.Items.Add("中心点");
            _cboMode.Items.Add("圆圈");
            _cboMode.Items.Add("方框瞄具");
            _cboMode.Items.Add("自定义图片");
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

            TextBlock imgHeader = new TextBlock { Text = "图片准星设置", FontWeight = FontWeights.Bold, Foreground = _themeAccentBrush, FontSize = 11 };
            _sectionHeaders.Add(imgHeader);
            imageStack.Children.Add(imgHeader);

            StackPanel imgBtnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            Button btnSelectImage = CreateButton("选择图片文件...", 150, 26, NeutralBtnColor);
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
                        _txtImageHVal.Text = targetH.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                        _selectedLayer.ImageHeight = targetH;
                        _isUpdatingUi = false;
                    }
                    UpdateAllViews();
                    _config.Save();
                }
            };
            imgBtnRow.Children.Add(btnSelectImage);
            imageStack.Children.Add(imgBtnRow);

            _txtCustomImagePath = new TextBlock { Text = "(未选择图片)", Foreground = Brushes.White, FontSize = 10, Margin = new Thickness(0, 0, 0, 4) };
            imageStack.Children.Add(_txtCustomImagePath);

            // ⭐ USER REQUEST: 纵横比锁定功能
            _chkLockAspectRatio = new CheckBox
            {
                Content = "锁定宽高比例",
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
                                _txtImageHVal.Text = targetH.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
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
                                _txtImageWVal.Text = targetW.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
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
            inspPanel.Children.Add(CreateSectionHeader("线条尺寸"));
            inspPanel.Children.Add(CreateSliderRow("线条长度 (px):", 0, 500, 10, out _sliderLength, out _txtLengthVal, val => { if (_selectedLayer != null) { _selectedLayer.Length = val; UpdateAllViews(); } }, true));
            inspPanel.Children.Add(CreateSliderRow("线条粗细 (px):", 0.1, 60, 2, out _sliderThickness, out _txtThicknessVal, val => { if (_selectedLayer != null) { _selectedLayer.Thickness = val; UpdateAllViews(); } }, true));
            inspPanel.Children.Add(CreateSliderRow("中心间隙 (px):", -50, 300, 5, out _sliderGap, out _txtGapVal, val => { if (_selectedLayer != null) { _selectedLayer.Gap = val; UpdateAllViews(); } }, true));

            // ⭐ USER REQUEST: 可以设置准星旋转 (0.000° ~ 360.000° 次像素平滑抗锯齿旋转)
            inspPanel.Children.Add(CreateSectionHeader("旋转角度"));
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
            inspPanel.Children.Add(CreateSectionHeader("线条颜色"));
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
            inspPanel.Children.Add(CreateSectionHeader("外描边轮廓"));
            _chkOutline = new CheckBox { Content = "启用外描边", Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) };
            _chkOutline.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowOutline = true; UpdateAllViews(); } };
            _chkOutline.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowOutline = false; UpdateAllViews(); } };
            inspPanel.Children.Add(_chkOutline);
            inspPanel.Children.Add(CreateSliderRow("描边粗细 (px):", 0.1, 20, 1, out _sliderOutline, out _txtOutlineVal, val => { if (_selectedLayer != null) { _selectedLayer.OutlineThickness = val; UpdateAllViews(); } }, true));

            // 6. Center Dot Settings
            inspPanel.Children.Add(CreateSectionHeader("中心瞄点"));
            _chkDot = new CheckBox { Content = "启用中心点", Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) };
            _chkDot.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowDot = true; UpdateAllViews(); } };
            _chkDot.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowDot = false; UpdateAllViews(); } };
            inspPanel.Children.Add(_chkDot);

            Grid dotShapeGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            dotShapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            dotShapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock lblDotShape = new TextBlock { Text = "形状:", Foreground = _themeAccentBrush, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            _cboDotShape = new ComboBox { Height = 24 };
            _cboDotShape.Items.Add("方形");
            _cboDotShape.Items.Add("圆形");
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

            inspPanel.Children.Add(CreateSliderRow("瞄点大小 (px):", 0.1, 60, 2, out _sliderDotSize, out _txtDotSizeVal, val => { if (_selectedLayer != null) { _selectedLayer.DotSize = val; UpdateAllViews(); } }, true));

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
            inspPanel.Children.Add(CreateSectionHeader("中心圆环"));
            _chkCircle = new CheckBox { Content = "启用中心圆环", Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) };
            _chkCircle.Checked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowCircle = true; UpdateAllViews(); } };
            _chkCircle.Unchecked += (s, e) => { if (!_isUpdatingUi && _selectedLayer != null) { _selectedLayer.ShowCircle = false; UpdateAllViews(); } };
            inspPanel.Children.Add(_chkCircle);
            inspPanel.Children.Add(CreateSliderRow("圆环半径 (px):", 0.5, 500, 20, out _sliderCircleRadius, out _txtCircleRadiusVal, val => { if (_selectedLayer != null) { _selectedLayer.CircleRadius = val; UpdateAllViews(); } }, true));
            inspPanel.Children.Add(CreateSliderRow("圆环线宽 (px):", 0.1, 50, 2, out _sliderCircleThick, out _txtCircleThickVal, val => { if (_selectedLayer != null) { _selectedLayer.CircleThickness = val; UpdateAllViews(); } }, true));

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
            inspPanel.Children.Add(CreateSectionHeader("按键自动隐蔽"));
            StackPanel layerMouseStack = new StackPanel { Margin = new Thickness(0, 2, 0, 8) };

            _chkLayerHideOnLeftMouse = new CheckBox
            {
                Content = "开火时隐蔽 (按住鼠标左键)",
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 4),
                ToolTip = "按住鼠标左键时临时隐藏此图层"
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
                Content = "开镜时隐蔽 (按住鼠标右键)",
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 4),
                ToolTip = "按住鼠标右键时临时隐藏此图层"
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

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_config.IsPositionLocked)
                {
                    _txtLockStatus.Text = "坐标已锁定，请先解锁。";
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
                _txtLockStatus.Text = _config.IsPositionLocked ? "坐标已锁定，点击右侧按钮解锁" : "坐标已解锁：可在上方预览框或屏幕上直接拖动";
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
                _btnRecordOverlayKey.Content = string.Format("【{0}】点击修改", _config.ToggleHotKeyName);
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
                _btnRecordSettingsKey.Content = string.Format("【{0}】点击修改", _config.SettingsHotKeyName);
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

    }
}
