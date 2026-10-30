using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CrosshairMaster.Models
{
    [DataContract]
    public class CrosshairGroup
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<CrosshairLayer> Layers { get; set; }

        public CrosshairGroup()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "新准星方案组";
            Layers = new List<CrosshairLayer>();
        }

        public CrosshairGroup Clone()
        {
            CrosshairGroup copy = new CrosshairGroup();
            copy.Id = Guid.NewGuid().ToString("N");
            copy.Name = this.Name + " (副本)";
            foreach (CrosshairLayer layer in this.Layers)
            {
                copy.Layers.Add(layer.Clone());
            }
            return copy;
        }
    }

    [DataContract]
    public class AppConfig
    {
        [DataMember]
        public bool OverlayEnabled { get; set; }

        [DataMember]
        public List<CrosshairLayer> Layers { get; set; }

        [DataMember]
        public string SelectedLayerId { get; set; }

        // Crosshair Groups / Profiles (替代老旧经典预设，支持组与列表自由拖拽流转)
        [DataMember]
        public List<CrosshairGroup> Groups { get; set; }

        [DataMember]
        public string SelectedGroupId { get; set; }

        // Position Lock state
        [DataMember]
        public bool IsPositionLocked { get; set; }

        // Mouse Hold Hiding (⭐ USER REQUEST: 支持按住左键时隐藏和按住右键时隐藏)
        [DataMember]
        public bool HideOnLeftMouse { get; set; }

        [DataMember]
        public bool HideOnRightMouse { get; set; }

        // Screen Resolution & Auto Center Adaptation (⭐ USER REQUEST: 分辨率变动时自动刷新软件)
        [DataMember]
        public bool AutoRefreshOnResolutionChange { get; set; }

        // Hotkeys
        [DataMember]
        public int ToggleHotKeyVK { get; set; } // default 0x77 (VK_F8)

        [DataMember]
        public string ToggleHotKeyName { get; set; } // e.g. "F8"

        [DataMember]
        public int ToggleHotKeyMod { get; set; }

        [DataMember]
        public int SettingsHotKeyVK { get; set; } // default 0x79 (VK_F10)

        [DataMember]
        public string SettingsHotKeyName { get; set; } // e.g. "F10"

        [DataMember]
        public int SettingsHotKeyMod { get; set; }

        // Custom Theme & Accent
        [DataMember]
        public string ThemePreset { get; set; }

        [DataMember]
        public string ThemeAccentHex { get; set; }

        // Detailed UI Color Customization (⭐ USER REQUEST: 所有每种UI都可以自定义颜色)
        [DataMember]
        public string UiBgColorHex { get; set; }

        [DataMember]
        public string UiCardColorHex { get; set; }

        [DataMember]
        public string UiHeaderColorHex { get; set; }

        [DataMember]
        public string UiBorderColorHex { get; set; }

        [DataMember]
        public string UiInputBgColorHex { get; set; }

        [DataMember]
        public string UiInputTextColorHex { get; set; }

        [DataMember]
        public string UiButtonBgColorHex { get; set; }

        [DataMember]
        public string UiDangerColorHex { get; set; }

        [DataMember]
        public string UiTextColorHex { get; set; }

        [DataMember]
        public string UiSubTextColorHex { get; set; }

        // Custom Preview Background
        [DataMember]
        public string PreviewBackground { get; set; }

        [DataMember]
        public string CustomBgImagePath { get; set; }

        // Custom MainWindow Background Wallpaper (⭐ USER REQUEST: 软件界面壁纸背景自定义)
        [DataMember]
        public string WindowBgImagePath { get; set; }

        [DataMember]
        public double WindowBgOpacity { get; set; }

        [DataMember]
        public bool ShowPixelGrid { get; set; }

        [DataMember]
        public bool PreviewOnlySelected { get; set; }

        [DataMember]
        public int PreviewZoom { get; set; }

        // ⭐ USER REQUEST: 三个大界面拖拽调比例记忆
        [DataMember]
        public double ColumnLeftRatio { get; set; }

        [DataMember]
        public double ColumnCenterRatio { get; set; }

        [DataMember]
        public double ColumnRightRatio { get; set; }

        [DataMember]
        public bool StartWithWindows { get; set; }

        public AppConfig()
        {
            OverlayEnabled = true;
            Layers = new List<CrosshairLayer>();
            Groups = new List<CrosshairGroup>();
            IsPositionLocked = false;
            HideOnLeftMouse = false;
            HideOnRightMouse = false;
            AutoRefreshOnResolutionChange = true;
            
            ToggleHotKeyVK = 0x77; // F8
            ToggleHotKeyName = "F8";
            ToggleHotKeyMod = 0;

            SettingsHotKeyVK = 0x79; // F10
            SettingsHotKeyName = "F10";
            SettingsHotKeyMod = 0;

            ThemePreset = "AppleDark";
            ThemeAccentHex = "#0A84FF";

            UiBgColorHex = "#1C1C1E";
            UiCardColorHex = "#2C2C2E";
            UiHeaderColorHex = "#242426";
            UiBorderColorHex = "#38383A";
            UiInputBgColorHex = "#1C1C1E";
            UiInputTextColorHex = "#FFFFFF";
            UiButtonBgColorHex = "#0A84FF";
            UiDangerColorHex = "#FF453A";
            UiTextColorHex = "#FFFFFF";
            UiSubTextColorHex = "#8E8E93";

            PreviewBackground = "Dark";
            CustomBgImagePath = "";
            WindowBgImagePath = "";
            WindowBgOpacity = 0.35;
            ShowPixelGrid = true;
            PreviewOnlySelected = true;
            PreviewZoom = 10;

            ColumnLeftRatio = 290.0;
            ColumnCenterRatio = 380.0;
            ColumnRightRatio = 480.0;

            StartWithWindows = false;
        }

        public static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "ScreenCrosshair");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "config.json");
        }

        public static AppConfig Load()
        {
            string path = GetConfigPath();
            if (File.Exists(path))
            {
                try
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppConfig));
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        AppConfig cfg = (AppConfig)serializer.ReadObject(fs);
                        if (cfg != null && cfg.Layers != null && cfg.Layers.Count > 0)
                        {
                            if (string.IsNullOrEmpty(cfg.ToggleHotKeyName)) cfg.ToggleHotKeyName = "F8";
                            if (string.IsNullOrEmpty(cfg.SettingsHotKeyName)) cfg.SettingsHotKeyName = "F10";
                            if (string.IsNullOrEmpty(cfg.ThemePreset) || cfg.ThemePreset == "CyberNeon") cfg.ThemePreset = "AppleDark";
                            if (string.IsNullOrEmpty(cfg.ThemeAccentHex) || cfg.ThemeAccentHex == "#00E5FF") cfg.ThemeAccentHex = "#0A84FF";

                            if (string.IsNullOrEmpty(cfg.UiBgColorHex)) cfg.UiBgColorHex = "#1C1C1E";
                            if (string.IsNullOrEmpty(cfg.UiCardColorHex)) cfg.UiCardColorHex = "#2C2C2E";
                            if (string.IsNullOrEmpty(cfg.UiHeaderColorHex)) cfg.UiHeaderColorHex = "#242426";
                            if (string.IsNullOrEmpty(cfg.UiBorderColorHex) || cfg.UiBorderColorHex == "#3A3A3E") cfg.UiBorderColorHex = "#38383A";
                            if (string.IsNullOrEmpty(cfg.UiInputBgColorHex) || cfg.UiInputBgColorHex == "#161618") cfg.UiInputBgColorHex = "#1C1C1E";
                            if (string.IsNullOrEmpty(cfg.UiInputTextColorHex)) cfg.UiInputTextColorHex = "#FFFFFF";
                            if (string.IsNullOrEmpty(cfg.UiButtonBgColorHex)) cfg.UiButtonBgColorHex = "#0A84FF";
                            if (string.IsNullOrEmpty(cfg.UiDangerColorHex)) cfg.UiDangerColorHex = "#FF453A";
                            if (string.IsNullOrEmpty(cfg.UiTextColorHex)) cfg.UiTextColorHex = "#FFFFFF";
                            if (string.IsNullOrEmpty(cfg.UiSubTextColorHex) || cfg.UiSubTextColorHex == "#A0A0B0") cfg.UiSubTextColorHex = "#8E8E93";

                            if (string.IsNullOrEmpty(cfg.PreviewBackground)) cfg.PreviewBackground = "Dark";
                            if (cfg.PreviewZoom <= 0) cfg.PreviewZoom = 10;
                            if (cfg.ColumnLeftRatio <= 50) cfg.ColumnLeftRatio = 290.0;
                            if (cfg.ColumnCenterRatio <= 50) cfg.ColumnCenterRatio = 380.0;
                            if (cfg.ColumnRightRatio <= 50) cfg.ColumnRightRatio = 480.0;
                            if (cfg.Groups == null)
                            {
                                cfg.Groups = new List<CrosshairGroup>();
                            }
                            // ⭐ USER REQUEST: 默认是仅选中的准星
                            try
                            {
                                string rawJson = File.ReadAllText(path);
                                if (!rawJson.Contains("\"PreviewOnlySelected\""))
                                {
                                    cfg.PreviewOnlySelected = true;
                                }
                                if (!rawJson.Contains("\"AutoRefreshOnResolutionChange\""))
                                {
                                    cfg.AutoRefreshOnResolutionChange = true;
                                }
                            }
                            catch { }
                            return cfg;
                        }
                    }
                }
                catch
                {
                }
            }

            AppConfig defaultCfg = CreateDefaultConfig();
            defaultCfg.Save();
            return defaultCfg;
        }

        public void Save()
        {
            try
            {
                string path = GetConfigPath();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppConfig));
                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, this);
                    byte[] jsonBytes = ms.ToArray();
                    File.WriteAllBytes(path, jsonBytes);
                }
            }
            catch
            {
            }
        }

        // ⭐ USER REQUEST: 自定义准星方案组都全部清空
        public static List<CrosshairGroup> CreateDefaultGroups()
        {
            return new List<CrosshairGroup>();
        }

        public static AppConfig CreateDefaultConfig()
        {
            AppConfig config = new AppConfig();
            config.Groups = new List<CrosshairGroup>();
            config.SelectedGroupId = "";

            // 默认提供一个干净基础十字准星
            CrosshairLayer defaultCrosshair = new CrosshairLayer
            {
                Name = "默认十字准星",
                LineMode = "Both",
                Length = 10,
                Thickness = 2,
                Gap = 4,
                ColorHex = "#00FF00",
                ShowOutline = true,
                OutlineThickness = 1,
                ShowDot = false
            };
            config.Layers.Add(defaultCrosshair);
            config.SelectedLayerId = defaultCrosshair.Id;
            return config;
        }
    }
}
