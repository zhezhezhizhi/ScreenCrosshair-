using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace MinimalIconBuilder
{
    class Program
    {
        static void Main()
        {
            string outDir = @"C:\Users\35357\.gemini\antigravity\scratch\CrosshairMaster\assets";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string icoPath = Path.Combine(outDir, "app.ico");
            string pngPath = Path.Combine(outDir, "app_icon.png");

            int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };

            // We generate the master 256x256 image first
            using (Bitmap masterBmp = RenderMinimalistCrosshair(256))
            {
                masterBmp.Save(pngPath, ImageFormat.Png);
            }

            // Create multi-resolution icon
            List<byte[]> dibEntries = new List<byte[]>();
            List<int> validSizes = new List<int>();

            foreach (int sz in sizes)
            {
                using (Bitmap bmp = RenderMinimalistCrosshair(sz))
                {
                    byte[] dib = CreateDibIconData(bmp);
                    dibEntries.Add(dib);
                    validSizes.Add(sz);
                }
            }

            using (FileStream fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR Header (6 bytes)
                bw.Write((ushort)0); // idReserved = 0
                bw.Write((ushort)1); // idType = 1 (Icon)
                bw.Write((ushort)validSizes.Count); // idCount

                int offset = 6 + (validSizes.Count * 16);

                for (int i = 0; i < validSizes.Count; i++)
                {
                    int sz = validSizes[i];
                    byte[] dib = dibEntries[i];
                    byte bW = (sz >= 256) ? (byte)0 : (byte)sz;
                    byte bH = (sz >= 256) ? (byte)0 : (byte)sz;

                    bw.Write(bW);
                    bw.Write(bH);
                    bw.Write((byte)0); // bColorCount = 0 for 32bpp
                    bw.Write((byte)0); // bReserved = 0
                    bw.Write((ushort)1); // wPlanes = 1
                    bw.Write((ushort)32); // wBitCount = 32
                    bw.Write((uint)dib.Length); // dwBytesInRes
                    bw.Write((uint)offset); // dwImageOffset

                    offset += dib.Length;
                }

                for (int i = 0; i < dibEntries.Count; i++)
                {
                    bw.Write(dibEntries[i]);
                }

                bw.Flush();
            }

            Console.WriteLine("Minimalist icon generated at " + icoPath);
        }

        // ⭐ 简约现代极简准星图案 (Clean, High-Tech, Minimalist Crosshair)
        public static Bitmap RenderMinimalistCrosshair(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float center = size / 2.0f;

                // 1. Sleek Minimal Dark Circular Backdrop
                float bgPad = size * 0.05f;
                float bgDiam = size - bgPad * 2.0f;
                using (SolidBrush darkBg = new SolidBrush(Color.FromArgb(245, 12, 16, 24)))
                {
                    g.FillEllipse(darkBg, bgPad, bgPad, bgDiam, bgDiam);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(220, 0, 229, 255), Math.Max(1.0f, size * 0.035f)))
                {
                    g.DrawEllipse(borderPen, bgPad, bgPad, bgDiam, bgDiam);
                }

                // 2. Minimalist Inner Precision Ring (Thin Neon Cyan)
                float ringR = size * 0.28f;
                float ringThick = Math.Max(1.0f, size * 0.03f);
                using (Pen ringPen = new Pen(Color.FromArgb(255, 0, 240, 255), ringThick))
                {
                    g.DrawEllipse(ringPen, center - ringR, center - ringR, ringR * 2, ringR * 2);
                }

                // 3. 4 Minimalist Crosshair Ticks (Clean perpendicular blades)
                float tickStart = ringR * 0.35f;
                float tickEnd = ringR * 1.35f;
                float tickThick = Math.Max(1.2f, size * 0.045f);
                using (Pen tickPen = new Pen(Color.FromArgb(255, 255, 255, 255), tickThick))
                {
                    tickPen.StartCap = LineCap.Flat;
                    tickPen.EndCap = LineCap.Flat;

                    // Horizontal ticks
                    g.DrawLine(tickPen, center - tickEnd, center, center - tickStart, center);
                    g.DrawLine(tickPen, center + tickStart, center, center + tickEnd, center);
                    // Vertical ticks
                    g.DrawLine(tickPen, center, center - tickEnd, center, center - tickStart);
                    g.DrawLine(tickPen, center, center + tickStart, center, center + tickEnd);
                }

                // 4. Center Dot (Ruby Red point)
                float dotR = Math.Max(1.5f, size * 0.075f);
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(255, 255, 42, 85)))
                {
                    g.FillEllipse(dotBrush, center - dotR, center - dotR, dotR * 2, dotR * 2);
                }
            }
            return bmp;
        }

        // Standard Windows DIB structure: BITMAPINFOHEADER + 32bpp XOR pixels (bottom-up) + 1bpp AND mask
        private static byte[] CreateDibIconData(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            int andStride = ((w + 31) / 32) * 4;
            int andSize = andStride * h;
            int xorSize = w * h * 4;
            int headerSize = 40; // BITMAPINFOHEADER
            int totalSize = headerSize + xorSize + andSize;

            byte[] buffer = new byte[totalSize];
            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // BITMAPINFOHEADER
                bw.Write((uint)40); // biSize
                bw.Write((int)w);   // biWidth
                bw.Write((int)(h * 2)); // biHeight (XOR + AND)
                bw.Write((ushort)1); // biPlanes
                bw.Write((ushort)32); // biBitCount
                bw.Write((uint)0); // biCompression (BI_RGB)
                bw.Write((uint)(xorSize + andSize)); // biSizeImage
                bw.Write((int)0); // biXPelsPerMeter
                bw.Write((int)0); // biYPelsPerMeter
                bw.Write((uint)0); // biClrUsed
                bw.Write((uint)0); // biClrImportant

                // XOR pixels: 32-bit BGRA, bottom-to-top
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    byte[] row = new byte[w * 4];
                    for (int y = h - 1; y >= 0; y--)
                    {
                        IntPtr rowPtr = new IntPtr(data.Scan0.ToInt64() + y * data.Stride);
                        System.Runtime.InteropServices.Marshal.Copy(rowPtr, row, 0, row.Length);
                        bw.Write(row);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }

                // AND mask: all 0s (for 32-bit ARGB icons, alpha channel handles transparency)
                byte[] andMask = new byte[andSize];
                bw.Write(andMask);
            }

            return buffer;
        }
    }
}
