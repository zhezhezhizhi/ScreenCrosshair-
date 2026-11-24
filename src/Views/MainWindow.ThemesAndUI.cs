using System.Text;
using System.Windows.Markup;
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
            ApplyScrollBarTheming();

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
                ToolTip = "点击选择颜色"
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
                ToolTip = "支持直接输入数值 (按回车生效)，可滑动滚轮微调"
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
    
        // ======================= 现代扁平极简滚动条 UI & 禁用原生右键菜单 =======================
        private void ApplyScrollBarTheming()
        {
            try
            {
                Style svStyle = new Style(typeof(ScrollViewer));
                svStyle.Setters.Add(new Setter(Control.ContextMenuProperty, null));
                svStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                svStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
                this.Resources[typeof(ScrollViewer)] = svStyle;

                string accentHex = _config.ThemeAccentHex ?? "#0A84FF";
                string xaml = string.Format(@"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:primitives=""clr-namespace:System.Windows.Controls.Primitives;assembly=PresentationFramework"">

    <Style x:Key=""ScrollBarPageButton"" TargetType=""{{x:Type RepeatButton}}"">
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""Transparent"" />
        <Setter Property=""Focusable"" Value=""False"" />
        <Setter Property=""IsTabStop"" Value=""False"" />
        <Setter Property=""ContextMenu"" Value=""{{x:Null}}"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{{x:Type RepeatButton}}"">
                    <Rectangle Fill=""Transparent"" />
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key=""ScrollBarThumbVertical"" TargetType=""{{x:Type Thumb}}"">
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""IsTabStop"" Value=""False"" />
        <Setter Property=""ContextMenu"" Value=""{{x:Null}}"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{{x:Type Thumb}}"">
                    <Border x:Name=""thumbBorder""
                            Background=""#48484A""
                            CornerRadius=""3.5""
                            Margin=""1,2,1,2"" />
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""#636366"" />
                        </Trigger>
                        <Trigger Property=""IsDragging"" Value=""True"">
                            <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{0}"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key=""ScrollBarThumbHorizontal"" TargetType=""{{x:Type Thumb}}"">
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""IsTabStop"" Value=""False"" />
        <Setter Property=""ContextMenu"" Value=""{{x:Null}}"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{{x:Type Thumb}}"">
                    <Border x:Name=""thumbBorder""
                            Background=""#48484A""
                            CornerRadius=""3.5""
                            Margin=""2,1,2,1"" />
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""#636366"" />
                        </Trigger>
                        <Trigger Property=""IsDragging"" Value=""True"">
                            <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{0}"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType=""{{x:Type ScrollBar}}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""ContextMenu"" Value=""{{x:Null}}"" />
        <Style.Triggers>
            <Trigger Property=""Orientation"" Value=""Vertical"">
                <Setter Property=""Width"" Value=""7"" />
                <Setter Property=""Height"" Value=""Auto"" />
                <Setter Property=""Template"">
                    <Setter.Value>
                        <ControlTemplate TargetType=""{{x:Type ScrollBar}}"">
                            <Grid Background=""Transparent"" ContextMenu=""{{x:Null}}"">
                                <Track x:Name=""PART_Track"" IsDirectionReversed=""True"">
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Style=""{{StaticResource ScrollBarPageButton}}"" Command=""primitives:ScrollBar.PageUpCommand"" />
                                    </Track.DecreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Style=""{{StaticResource ScrollBarThumbVertical}}"" />
                                    </Track.Thumb>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Style=""{{StaticResource ScrollBarPageButton}}"" Command=""primitives:ScrollBar.PageDownCommand"" />
                                    </Track.IncreaseRepeatButton>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Trigger>
            <Trigger Property=""Orientation"" Value=""Horizontal"">
                <Setter Property=""Width"" Value=""Auto"" />
                <Setter Property=""Height"" Value=""7"" />
                <Setter Property=""Template"">
                    <Setter.Value>
                        <ControlTemplate TargetType=""{{x:Type ScrollBar}}"">
                            <Grid Background=""Transparent"" ContextMenu=""{{x:Null}}"">
                                <Track x:Name=""PART_Track"" IsDirectionReversed=""False"">
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Style=""{{StaticResource ScrollBarPageButton}}"" Command=""primitives:ScrollBar.PageLeftCommand"" />
                                    </Track.DecreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Style=""{{StaticResource ScrollBarThumbHorizontal}}"" />
                                    </Track.Thumb>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Style=""{{StaticResource ScrollBarPageButton}}"" Command=""primitives:ScrollBar.PageRightCommand"" />
                                    </Track.IncreaseRepeatButton>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Trigger>
        </Style.Triggers>
    </Style>
</ResourceDictionary>
", accentHex);

                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    ResourceDictionary dict = (ResourceDictionary)XamlReader.Load(ms);
                    this.Resources.MergedDictionaries.Add(dict);
                    if (dict.Contains(typeof(ScrollBar)))
                    {
                        this.Resources[typeof(ScrollBar)] = dict[typeof(ScrollBar)];
                    }
                }
            }
            catch { }
        }
    }
}