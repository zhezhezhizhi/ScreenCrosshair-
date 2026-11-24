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
                    Text = "(暂无方案，点击上方新建)",
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
                    ToolTip = "双击重命名"
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

                Button btnRenameInGroup = CreateMiniButton("改名", "重命名准星", NeutralBtnColor);
                btnRenameInGroup.Width = 46;
                btnRenameInGroup.Click += (s, e) =>
                {
                    RenameGroupLayer(layer);
                };

                Button btnImportToScreen = CreateMiniButton("添加", "添加到当前屏幕", NeutralBtnColor);
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

                Button btnDelFromGroup = CreateMiniButton("✕", "删除", DangerBtnColor);
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
            string newName = Microsoft.VisualBasic.Interaction.InputBox("请输入准星名称:", "重命名准星", layer.Name);
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
            string defName = string.Format("方案 {0}", _config.Groups.Count + 1);
            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入方案名称:", "新建方案", defName);
            g.Name = (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(input.Trim())) ? input.Trim() : defName;
            _config.Groups.Add(g);
            _selectedGroup = g;
            RefreshGroupList();
            _config.Save();
        }

        private void RenameCurrentGroup()
        {
            if (_selectedGroup == null) return;
            string newName = Microsoft.VisualBasic.Interaction.InputBox("请输入方案名称:", "重命名方案", _selectedGroup.Name);
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
                MessageBox.Show("当前方案中没有图层", "ScreenCrosshair", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(string.Format("已保存至方案「{0}」", _selectedGroup.Name), "ScreenCrosshair", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
