using System;
using System.Runtime.Serialization;

namespace CrosshairMaster.Models
{
    [DataContract]
    public class CrosshairLayer
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        // Mode: Both (十字), HorizontalOnly (仅横线), VerticalOnly (仅竖线), TShape (T字), DotOnly (纯点), CircleOnly (纯圆), Brackets (括号)
        [DataMember]
        public string LineMode { get; set; }

        [DataMember]
        public double Length { get; set; }

        [DataMember]
        public double Thickness { get; set; }

        [DataMember]
        public double Gap { get; set; }

        [DataMember]
        public string ColorHex { get; set; }

        [DataMember]
        public double Alpha { get; set; }

        [DataMember]
        public bool ShowOutline { get; set; }

        [DataMember]
        public double OutlineThickness { get; set; }

        [DataMember]
        public string OutlineColorHex { get; set; }

        [DataMember]
        public bool ShowDot { get; set; }

        [DataMember]
        public double DotSize { get; set; }

        [DataMember]
        public string DotShape { get; set; } // "Square" (像素方块 - 推荐默认) or "Circle" (圆形)

        [DataMember]
        public string DotColorHex { get; set; }

        [DataMember]
        public bool DotOutline { get; set; }

        [DataMember]
        public bool ShowCircle { get; set; }

        [DataMember]
        public double CircleRadius { get; set; }

        [DataMember]
        public double CircleThickness { get; set; }

        [DataMember]
        public string CircleColorHex { get; set; }

        [DataMember]
        public bool CircleOutline { get; set; }

        [DataMember]
        public double OffsetX { get; set; }

        [DataMember]
        public double OffsetY { get; set; }

        // Rotation (⭐ USER REQUEST: 可以设置准星旋转)
        [DataMember]
        public double Rotation { get; set; }

        // Mouse Hold Hiding (⭐ USER REQUEST: 只对勾选的单一图层隐藏)
        [DataMember]
        public bool HideOnLeftMouse { get; set; }

        [DataMember]
        public bool HideOnRightMouse { get; set; }

        // Custom Image Reticle (⭐ USER REQUEST: 支持导入图片作为准星)
        [DataMember]
        public string CustomImagePath { get; set; }

        [DataMember]
        public double ImageWidth { get; set; }

        [DataMember]
        public double ImageHeight { get; set; }

        [DataMember]
        public double ImageRotation { get; set; }

        // ⭐ USER REQUEST: 纵横比锁定 (锁定则保持原比例，未锁定则按宽高压缩/拉伸)
        [DataMember]
        public bool LockAspectRatio { get; set; }

        // Asymmetric arm lengths (optional advanced feature)
        [DataMember]
        public bool CustomArms { get; set; }

        [DataMember]
        public double LengthTop { get; set; }

        [DataMember]
        public double LengthBottom { get; set; }

        [DataMember]
        public double LengthLeft { get; set; }

        [DataMember]
        public double LengthRight { get; set; }

        public CrosshairLayer()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "新准星图层";
            Enabled = true;
            LineMode = "Both";
            Length = 12;
            Thickness = 2;
            Gap = 5;
            ColorHex = "#00FF00";
            Alpha = 1.0;
            ShowOutline = true;
            OutlineThickness = 1;
            OutlineColorHex = "#000000";

            ShowDot = false;
            DotSize = 2;
            DotShape = "Square"; // Default to square pixel dot!
            DotColorHex = "#FF0000";
            DotOutline = true;

            ShowCircle = false;
            CircleRadius = 20;
            CircleThickness = 2;
            CircleColorHex = "#00FF00";
            CircleOutline = false;

            OffsetX = 0;
            OffsetY = 0;
            Rotation = 0;
            HideOnLeftMouse = false;
            HideOnRightMouse = false;

            CustomImagePath = "";
            ImageWidth = 32;
            ImageHeight = 32;
            ImageRotation = 0;
            LockAspectRatio = true;

            CustomArms = false;
            LengthTop = 12;
            LengthBottom = 12;
            LengthLeft = 12;
            LengthRight = 12;
        }

        public CrosshairLayer Clone()
        {
            CrosshairLayer clone = (CrosshairLayer)this.MemberwiseClone();
            clone.Id = Guid.NewGuid().ToString("N");
            clone.Name = this.Name + " (副本)";
            return clone;
        }
    }
}
