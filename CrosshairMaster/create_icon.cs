using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace IconGenerator
{
    class Program
    {
        static void Main()
        {
            string outDir = @"C:\Users\35357\.gemini\antigravity\scratch\CrosshairMaster\assets";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string icoPath = Path.Combine(outDir, "app.ico");

            int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
            List<byte[]> pngs = new List<byte[]>();

            foreach (int sz in sizes)
            {
                using (Bitmap bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.Clear(Color.Transparent);

                        float center = sz / 2.0f;
                        float pad = sz * 0.05f;
                        float boxSize = sz - pad * 2.0f;
                        float radius = sz * 0.22f;

                        // 1. Dark squircle shield base
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            path.AddArc(pad, pad, radius * 2, radius * 2, 180, 90);
                            path.AddArc(pad + boxSize - radius * 2, pad, radius * 2, radius * 2, 270, 90);
                            path.AddArc(pad + boxSize - radius * 2, pad + boxSize - radius * 2, radius * 2, radius * 2, 0, 90);
                            path.AddArc(pad, pad + boxSize - radius * 2, radius * 2, radius * 2, 90, 90);
                            path.CloseFigure();

                            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                                new PointF(0, 0), new PointF(sz, sz),
                                Color.FromArgb(255, 18, 22, 34), Color.FromArgb(255, 10, 12, 18)))
                            {
                                g.FillPath(bgBrush, path);
                            }

                            using (Pen borderPen = new Pen(Color.FromArgb(220, 0, 229, 255), Math.Max(1.0f, sz * 0.04f)))
                            {
                                g.DrawPath(borderPen, path);
                            }
                        }

                        // 2. Neon Cyan Tactical Outer Ring
                        float reticleR = sz * 0.32f;
                        float ringThick = Math.Max(1.0f, sz * 0.035f);
                        using (Pen cyanPen = new Pen(Color.FromArgb(240, 0, 229, 255), ringThick))
                        {
                            g.DrawEllipse(cyanPen, center - reticleR, center - reticleR, reticleR * 2, reticleR * 2);
                        }

                        // 3. Crisp Crosshair Blades
                        float bladeGap = reticleR * 0.28f;
                        float bladeLen = reticleR * 0.85f;
                        float bladeThick = Math.Max(1.2f, sz * 0.05f);
                        using (Pen bladePen = new Pen(Color.FromArgb(255, 0, 240, 255), bladeThick))
                        {
                            // Left & Right
                            g.DrawLine(bladePen, center - bladeLen, center, center - bladeGap, center);
                            g.DrawLine(bladePen, center + bladeGap, center, center + bladeLen, center);
                            // Top & Bottom
                            g.DrawLine(bladePen, center, center - bladeLen, center, center - bladeGap);
                            g.DrawLine(bladePen, center, center + bladeGap, center, center + bladeLen);
                        }

                        // 4. Center Precision Dot (Bright ruby red with crisp outline)
                        float dotR = Math.Max(1.5f, sz * 0.08f);
                        using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(255, 255, 42, 85)))
                        {
                            g.FillEllipse(dotBrush, center - dotR, center - dotR, dotR * 2, dotR * 2);
                        }
                        using (Pen dotBorder = new Pen(Color.FromArgb(255, 255, 255, 255), Math.Max(0.8f, sz * 0.02f)))
                        {
                            g.DrawEllipse(dotBorder, center - dotR, center - dotR, dotR * 2, dotR * 2);
                        }
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        pngs.Add(ms.ToArray());
                    }
                }
            }

            // Write Windows ICO format
            using (FileStream fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((ushort)0); // Reserved
                bw.Write((ushort)1); // Type 1 = Icon
                bw.Write((ushort)sizes.Length); // Image count

                int offset = 6 + (sizes.Length * 16);

                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    byte[] data = pngs[i];
                    byte bW = (sz >= 256) ? (byte)0 : (byte)sz;
                    byte bH = (sz >= 256) ? (byte)0 : (byte)sz;

                    bw.Write(bW);
                    bw.Write(bH);
                    bw.Write((byte)0); // Color count
                    bw.Write((byte)0); // Reserved
                    bw.Write((ushort)1); // Planes
                    bw.Write((ushort)32); // Bits per pixel
                    bw.Write((uint)data.Length); // Image size in bytes
                    bw.Write((uint)offset); // Offset in file

                    offset += data.Length;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngs[i]);
                }

                bw.Flush();
            }

            Console.WriteLine("app.ico generated successfully!");
        }
    }
}
