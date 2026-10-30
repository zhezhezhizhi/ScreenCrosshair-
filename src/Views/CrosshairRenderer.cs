using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CrosshairMaster.Models;

namespace CrosshairMaster.Views
{
    public static class CrosshairRenderer
    {
        private static Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        public static SolidColorBrush ParseBrush(string hex, double alpha)
        {
            try
            {
                if (string.IsNullOrEmpty(hex))
                {
                    hex = "#00FF00";
                }
                Color c = (Color)ColorConverter.ConvertFromString(hex);
                c.A = (byte)Math.Max(0, Math.Min(255, (int)(c.A * Math.Max(0.0, Math.Min(1.0, alpha)))));
                SolidColorBrush brush = new SolidColorBrush(c);
                brush.Freeze();
                return brush;
            }
            catch
            {
                return Brushes.Lime;
            }
        }

        public static BitmapImage GetCachedBitmap(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            if (_imageCache.ContainsKey(path))
            {
                return _imageCache[path];
            }

            try
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                _imageCache[path] = bi;
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public static void RenderLayers(Canvas canvas, IEnumerable<CrosshairLayer> layers, double originX, double originY, double scale = 1.0, bool showPixelGrid = false, bool applyOffset = true)
        {
            canvas.Children.Clear();
            if (layers == null) return;

            // 1. Draw Pixel Grid if zoomed in and requested
            if (showPixelGrid && scale >= 4.0)
            {
                DrawPixelGrid(canvas, originX, originY, scale);
            }

            // 2. Render all active layers
            foreach (CrosshairLayer layer in layers)
            {
                if (layer.Enabled)
                {
                    RenderSingleLayer(canvas, layer, originX, originY, scale, applyOffset);
                }
            }

            // 3. Draw Center Reference Marker on high zoom
            if (showPixelGrid && scale >= 6.0)
            {
                DrawCenterMarker(canvas, originX, originY, scale);
            }
        }

        public static void RenderSingleLayer(Canvas canvas, CrosshairLayer layer, double originX, double originY, double scale = 1.0, bool applyOffset = true)
        {
            if (layer == null) return;

            // ⭐ Sub-pixel exact coordinates (绝不进行 Math.Round 整数截断，完美支持 0.001 - 0.5 之间的次像素平滑抗锯齿微调)
            double cx = applyOffset ? (originX + layer.OffsetX * scale) : originX;
            double cy = applyOffset ? (originY + layer.OffsetY * scale) : originY;

            // ⭐ Crosshair Rotation (支持任意图层 0.000° ~ 360.000° 旋转)
            Canvas layerContainer = canvas;
            if (Math.Abs(layer.Rotation) > 0.0001)
            {
                layerContainer = new Canvas();
                layerContainer.RenderTransform = new RotateTransform(layer.Rotation, cx, cy);
                canvas.Children.Add(layerContainer);
            }

            SolidColorBrush mainBrush = ParseBrush(layer.ColorHex, layer.Alpha);
            SolidColorBrush outlineBrush = ParseBrush(layer.OutlineColorHex, layer.Alpha);
            SolidColorBrush dotBrush = ParseBrush(layer.DotColorHex, layer.Alpha);
            SolidColorBrush circleBrush = ParseBrush(layer.CircleColorHex, layer.Alpha);

            double L = Math.Max(0.0, layer.Length);
            double T = Math.Max(0.1, layer.Thickness);
            // ⭐ Allow negative gap! (间隙数值可为负数)
            double G = layer.Gap;
            double O = layer.ShowOutline ? Math.Max(0.1, layer.OutlineThickness) : 0.0;

            string mode = layer.LineMode ?? "Both";
            double scaledT = T * scale;
            double scaledO = O * scale;
            double halfT = (T / 2.0) * scale;

            // ================= 1. Custom Image Reticle (⭐ USER REQUEST: 支持导入图片作为准星 & 纵横比锁定) =================
            if (mode == "CustomImage" || (!string.IsNullOrEmpty(layer.CustomImagePath) && mode == "Image"))
            {
                BitmapImage img = GetCachedBitmap(layer.CustomImagePath);
                if (img != null)
                {
                    double imgW = (layer.ImageWidth > 0 ? layer.ImageWidth : 32) * scale;
                    double imgH = (layer.ImageHeight > 0 ? layer.ImageHeight : 32) * scale;

                    Image imgControl = new Image
                    {
                        Source = img,
                        Width = imgW,
                        Height = imgH,
                        Opacity = Math.Max(0.1, Math.Min(1.0, layer.Alpha)),
                        // 锁定纵横比则保持比例，未锁定则按对应长宽强制拉伸或压缩
                        Stretch = layer.LockAspectRatio ? Stretch.Uniform : Stretch.Fill,
                        SnapsToDevicePixels = false
                    };

                    RenderOptions.SetBitmapScalingMode(imgControl, BitmapScalingMode.HighQuality);
                    double totalRot = layer.Rotation + layer.ImageRotation;
                    if (Math.Abs(totalRot) > 0.0001)
                    {
                        imgControl.RenderTransformOrigin = new Point(0.5, 0.5);
                        imgControl.RenderTransform = new RotateTransform(totalRot);
                    }

                    Canvas.SetLeft(imgControl, cx - imgW / 2.0);
                    Canvas.SetTop(imgControl, cy - imgH / 2.0);
                    canvas.Children.Add(imgControl);
                    return; // Done rendering image
                }
            }

            // ================= 2. Unified Coplanar Lines (⭐ USER REQUEST: 同一个平面几何，绝无上下堆叠/黑线切割) =================
            if (mode == "SolidHorizontal" && L > 0)
            {
                double totalW = L * 2 * scale;
                Rect rH = MakePixelRect(cx - L * scale, cy - halfT, totalW, scaledT);
                DrawUnifiedGeom(layerContainer, new RectangleGeometry(rH), scaledO, mainBrush, outlineBrush);
            }
            else if (mode == "SolidVertical" && L > 0)
            {
                double totalH = L * 2 * scale;
                Rect rV = MakePixelRect(cx - halfT, cy - L * scale, scaledT, totalH);
                DrawUnifiedGeom(layerContainer, new RectangleGeometry(rV), scaledO, mainBrush, outlineBrush);
            }
            else if (mode == "SolidCross" && L > 0)
            {
                double totalW = L * 2 * scale;
                double totalH = L * 2 * scale;
                Rect rH = MakePixelRect(cx - L * scale, cy - halfT, totalW, scaledT);
                Rect rV = MakePixelRect(cx - halfT, cy - L * scale, scaledT, totalH);
                CombinedGeometry cg = new CombinedGeometry(GeometryCombineMode.Union, new RectangleGeometry(rH), new RectangleGeometry(rV));
                DrawUnifiedGeom(layerContainer, cg, scaledO, mainBrush, outlineBrush);
            }
            else if (mode == "XCross" && L > 0)
            {
                DrawUnifiedXCross(layerContainer, cx, cy, L * scale, scaledT, scaledO, mainBrush, outlineBrush);
            }
            else if (mode == "Chevron" && L > 0)
            {
                DrawChevron(layerContainer, cx, cy, L * scale, scaledT, scaledO, mainBrush, outlineBrush);
            }
            else
            {
                // Standard Split Arms (Both, HorizontalOnly, VerticalOnly, TShape, Brackets)
                bool drawHorizontal = (mode == "Both" || mode == "HorizontalOnly" || mode == "TShape");
                bool drawTop = (mode == "Both" || mode == "VerticalOnly");
                bool drawBottom = (mode == "Both" || mode == "VerticalOnly" || mode == "TShape");
                bool drawBrackets = (mode == "Brackets");

                List<Rect> armRects = new List<Rect>();

                // Left Arm
                if (drawHorizontal && L > 0)
                {
                    double w = L * scale;
                    double x = cx - (G + L) * scale;
                    double y = cy - halfT;
                    armRects.Add(MakePixelRect(x, y, w, scaledT));
                }

                // Right Arm
                if (drawHorizontal && L > 0)
                {
                    double w = L * scale;
                    double x = cx + G * scale;
                    double y = cy - halfT;
                    armRects.Add(MakePixelRect(x, y, w, scaledT));
                }

                // Top Arm
                if (drawTop && L > 0)
                {
                    double h = L * scale;
                    double x = cx - halfT;
                    double y = cy - (G + L) * scale;
                    armRects.Add(MakePixelRect(x, y, scaledT, h));
                }

                // Bottom Arm
                if (drawBottom && L > 0)
                {
                    double h = L * scale;
                    double x = cx - halfT;
                    double y = cy + G * scale;
                    armRects.Add(MakePixelRect(x, y, scaledT, h));
                }

                // Brackets Mode
                if (drawBrackets && L > 0)
                {
                    double tipLen = Math.Max(0.5, L * 0.4) * scale;
                    double armH = L * scale;
                    double halfH = (L / 2.0) * scale;

                    double lx = cx - (G + T) * scale;
                    double ly = cy - halfH;
                    armRects.Add(MakePixelRect(lx, ly, scaledT, armH));
                    armRects.Add(MakePixelRect(lx, ly, tipLen, scaledT));
                    armRects.Add(MakePixelRect(lx, ly + armH - scaledT, tipLen, scaledT));

                    double rx = cx + G * scale;
                    double ry = cy - halfH;
                    armRects.Add(MakePixelRect(rx, ry, scaledT, armH));
                    armRects.Add(MakePixelRect(rx + scaledT - tipLen, ry, tipLen, scaledT));
                    armRects.Add(MakePixelRect(rx + scaledT - tipLen, ry + armH - scaledT, tipLen, scaledT));
                }

                if (armRects.Count > 0)
                {
                    Geometry unionArms = CombineRects(armRects);
                    DrawUnifiedGeom(layerContainer, unionArms, scaledO, mainBrush, outlineBrush);
                }
            }

            // ================= 3. Center Circle (⭐ Fix: CircleOnly always draws) =================
            bool shouldDrawCircle = (layer.ShowCircle || mode == "CircleOnly");
            double circleRadius = layer.CircleRadius > 0 ? layer.CircleRadius : 20.0;
            double circleThick = layer.CircleThickness > 0 ? layer.CircleThickness : 2.0;

            if (shouldDrawCircle && circleRadius > 0)
            {
                double r = circleRadius * scale;
                double ct = Math.Max(0.1, circleThick * scale);
                double outOffset = O * scale;

                bool circleHasOutline = (layer.CircleOutline || layer.ShowOutline) && O > 0;
                if (circleHasOutline)
                {
                    Ellipse circleOut = new Ellipse
                    {
                        Width = (r + outOffset) * 2,
                        Height = (r + outOffset) * 2,
                        Stroke = outlineBrush,
                        StrokeThickness = ct + outOffset * 2,
                        SnapsToDevicePixels = false
                    };
                    Canvas.SetLeft(circleOut, cx - (r + outOffset));
                    Canvas.SetTop(circleOut, cy - (r + outOffset));
                    layerContainer.Children.Add(circleOut);
                }

                Ellipse circle = new Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Stroke = circleBrush,
                    StrokeThickness = ct,
                    SnapsToDevicePixels = false
                };
                Canvas.SetLeft(circle, cx - r);
                Canvas.SetTop(circle, cy - r);
                layerContainer.Children.Add(circle);
            }

            // ================= 4. Center Dot (⭐ Fix: DotOnly always draws) =================
            bool shouldDrawDot = (layer.ShowDot || mode == "DotOnly");
            double dotSize = layer.DotSize > 0 ? layer.DotSize : 2.0;

            if (shouldDrawDot && dotSize > 0)
            {
                double dotRenderSize = Math.Max(0.1, dotSize * scale);
                double dotHalf = dotRenderSize / 2.0;
                double dotLeft = cx - dotHalf;
                double dotTop = cy - dotHalf;

                string dotShape = layer.DotShape ?? "Square";

                if (dotShape == "Square")
                {
                    bool dotHasOutline = (layer.DotOutline || layer.ShowOutline) && O > 0;
                    if (dotHasOutline)
                    {
                        Rectangle dotOut = new Rectangle
                        {
                            Width = dotRenderSize + scaledO * 2,
                            Height = dotRenderSize + scaledO * 2,
                            Fill = outlineBrush,
                            SnapsToDevicePixels = false
                        };
                        Canvas.SetLeft(dotOut, dotLeft - scaledO);
                        Canvas.SetTop(dotOut, dotTop - scaledO);
                        layerContainer.Children.Add(dotOut);
                    }

                    Rectangle dotSquare = new Rectangle
                    {
                        Width = dotRenderSize,
                        Height = dotRenderSize,
                        Fill = dotBrush,
                        SnapsToDevicePixels = false
                    };
                    Canvas.SetLeft(dotSquare, dotLeft);
                    Canvas.SetTop(dotSquare, dotTop);
                    layerContainer.Children.Add(dotSquare);
                }
                else
                {
                    double r = (dotRenderSize / 2.0);
                    bool dotHasOutline = (layer.DotOutline || layer.ShowOutline) && O > 0;
                    if (dotHasOutline)
                    {
                        Ellipse dotOut = new Ellipse
                        {
                            Width = (r + scaledO) * 2,
                            Height = (r + scaledO) * 2,
                            Fill = outlineBrush,
                            SnapsToDevicePixels = false
                        };
                        Canvas.SetLeft(dotOut, cx - (r + scaledO));
                        Canvas.SetTop(dotOut, cy - (r + scaledO));
                        layerContainer.Children.Add(dotOut);
                    }

                    Ellipse dotCircle = new Ellipse
                    {
                        Width = r * 2,
                        Height = r * 2,
                        Fill = dotBrush,
                        SnapsToDevicePixels = false
                    };
                    Canvas.SetLeft(dotCircle, cx - r);
                    Canvas.SetTop(dotCircle, cy - r);
                    layerContainer.Children.Add(dotCircle);
                }
            }
        }

        private static Rect MakePixelRect(double x, double y, double w, double h)
        {
            return new Rect(x, y, Math.Max(0.1, w), Math.Max(0.1, h));
        }

        private static Geometry CombineRects(IEnumerable<Rect> rects)
        {
            Geometry unionGeom = null;
            foreach (Rect r in rects)
            {
                RectangleGeometry rg = new RectangleGeometry(r);
                if (unionGeom == null)
                {
                    unionGeom = rg;
                }
                else
                {
                    unionGeom = new CombinedGeometry(GeometryCombineMode.Union, unionGeom, rg);
                }
            }
            return unionGeom;
        }

        // ⭐ USER REQUEST: 单一平面几何渲染 (彻底解决上下堆叠、交叉处黑线切割接缝问题)
        private static void DrawUnifiedGeom(Canvas canvas, Geometry geom, double outline, Brush mainBrush, Brush outlineBrush)
        {
            if (geom == null) return;

            if (outline > 0 && outlineBrush != null)
            {
                // 使用裁切确保黑描边绝不向内渗入到几何内部，保证彩色区域为纯净统一平面
                RectangleGeometry hugeRect = new RectangleGeometry(new Rect(-10000, -10000, 20000, 20000));
                CombinedGeometry outerClip = new CombinedGeometry(GeometryCombineMode.Exclude, hugeRect, geom);

                System.Windows.Shapes.Path outlinePath = new System.Windows.Shapes.Path
                {
                    Data = geom,
                    Stroke = outlineBrush,
                    StrokeThickness = outline * 2.0,
                    StrokeLineJoin = PenLineJoin.Miter,
                    Clip = outerClip,
                    SnapsToDevicePixels = false
                };
                canvas.Children.Add(outlinePath);
            }

            System.Windows.Shapes.Path fillPath = new System.Windows.Shapes.Path
            {
                Data = geom,
                Fill = mainBrush,
                SnapsToDevicePixels = false
            };
            canvas.Children.Add(fillPath);
        }

        private static void DrawUnifiedXCross(Canvas canvas, double cx, double cy, double length, double thick, double outline, Brush mainBrush, Brush outlineBrush)
        {
            double rad = 45.0 * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            double x1 = cx - length * cos, y1 = cy - length * sin, x2 = cx + length * cos, y2 = cy + length * sin;
            double x3 = cx - length * cos, y3 = cy + length * sin, x4 = cx + length * cos, y4 = cy - length * sin;

            // 先统一绘制所有外描边，再统一绘制前景填充，杜绝交叉堆叠切线
            if (outline > 0 && outlineBrush != null)
            {
                canvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = outlineBrush, StrokeThickness = thick + outline * 2, StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square, SnapsToDevicePixels = false });
                canvas.Children.Add(new Line { X1 = x3, Y1 = y3, X2 = x4, Y2 = y4, Stroke = outlineBrush, StrokeThickness = thick + outline * 2, StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square, SnapsToDevicePixels = false });
            }

            canvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = mainBrush, StrokeThickness = thick, StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square, SnapsToDevicePixels = false });
            canvas.Children.Add(new Line { X1 = x3, Y1 = y3, X2 = x4, Y2 = y4, Stroke = mainBrush, StrokeThickness = thick, StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square, SnapsToDevicePixels = false });
        }

        private static void DrawChevron(Canvas canvas, double cx, double cy, double size, double thick, double outline, Brush mainBrush, Brush outlineBrush)
        {
            // Inverted V shape
            Point pTop = new Point(cx, cy - size * 0.5);
            Point pLeft = new Point(cx - size * 0.7, cy + size * 0.5);
            Point pRight = new Point(cx + size * 0.7, cy + size * 0.5);

            if (outline > 0)
            {
                Polyline outPoly = new Polyline
                {
                    Points = new PointCollection { pLeft, pTop, pRight },
                    Stroke = outlineBrush,
                    StrokeThickness = thick + outline * 2,
                    StrokeLineJoin = PenLineJoin.Miter,
                    SnapsToDevicePixels = false
                };
                canvas.Children.Add(outPoly);
            }

            Polyline fg = new Polyline
            {
                Points = new PointCollection { pLeft, pTop, pRight },
                Stroke = mainBrush,
                StrokeThickness = thick,
                StrokeLineJoin = PenLineJoin.Miter,
                SnapsToDevicePixels = false
            };
            canvas.Children.Add(fg);
        }

        public static void DrawPixelGrid(Canvas canvas, double cx, double cy, double scale)
        {
            double w = canvas.Width > 0 ? canvas.Width : 340;
            double h = canvas.Height > 0 ? canvas.Height : 270;

            SolidColorBrush gridBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            gridBrush.Freeze();

            double step = scale;
            for (double x = cx; x < w; x += step)
            {
                Line l = new Line { X1 = Math.Round(x), Y1 = 0, X2 = Math.Round(x), Y2 = h, Stroke = gridBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
                canvas.Children.Add(l);
            }
            for (double x = cx - step; x >= 0; x -= step)
            {
                Line l = new Line { X1 = Math.Round(x), Y1 = 0, X2 = Math.Round(x), Y2 = h, Stroke = gridBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
                canvas.Children.Add(l);
            }

            for (double y = cy; y < h; y += step)
            {
                Line l = new Line { X1 = 0, Y1 = Math.Round(y), X2 = w, Y2 = Math.Round(y), Stroke = gridBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
                canvas.Children.Add(l);
            }
            for (double y = cy - step; y >= 0; y -= step)
            {
                Line l = new Line { X1 = 0, Y1 = Math.Round(y), X2 = w, Y2 = Math.Round(y), Stroke = gridBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
                canvas.Children.Add(l);
            }
        }

        public static void DrawCenterMarker(Canvas canvas, double cx, double cy, double scale)
        {
            SolidColorBrush centerBrush = new SolidColorBrush(Color.FromArgb(180, 0, 229, 255));
            centerBrush.Freeze();

            Line ch = new Line { X1 = cx - scale * 4, Y1 = cy, X2 = cx + scale * 4, Y2 = cy, Stroke = centerBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
            Line cv = new Line { X1 = cx, Y1 = cy - scale * 4, X2 = cx, Y2 = cy + scale * 4, Stroke = centerBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
            canvas.Children.Add(ch);
            canvas.Children.Add(cv);
        }
    }
}
