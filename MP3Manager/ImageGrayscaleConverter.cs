using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Manager
{
    public enum DitherMode { None, OrderedBayer2, OrderedBayer4, OrderedBayer8, FloydSteinberg, Atkinson }

    public sealed class TwoBppResult : IDisposable
    {
        public Bitmap DisplayBitmap { get; set; }   // 8bpp indexed, 4-shade palette
        public byte[] Packed2Bpp { get; set; }      // 2 bits per pixel, row-major
        public void Dispose() { if (DisplayBitmap != null) DisplayBitmap.Dispose(); }
    }

    public struct KernelWeight
    {
        public int Dx, Dy; public float W;
        public KernelWeight(int dx, int dy, float w) { Dx = dx; Dy = dy; W = w; }
    }

    internal class ImageGrayscaleConverter
    {
        private static readonly byte[] Shades = { 0, 85, 170, 255 };

        public static TwoBppResult Convert(Bitmap source, DitherMode mode)
        {
            int w = source.Width, h = source.Height;

            // Always work from a 32bpp clone (safe for any input format)
            Bitmap src32 = source.Clone(new Rectangle(0, 0, w, h), PixelFormat.Format32bppArgb);

            // 8bpp indexed preview bitmap
            Bitmap disp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            SetFourShadePalette(disp);

            BitmapData srcData = null, dstData = null;

            try
            {
                srcData = src32.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                dstData = disp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;

                // grayscale work buffer (float for diffusion)
                float[] gray = new float[w * h];

                unsafe
                {
                    byte* srcBase = (byte*)srcData.Scan0.ToPointer();
                    for (int y = 0; y < h; y++)
                    {
                        byte* s = srcBase + y * srcStride;
                        int off = y * w;
                        for (int x = 0; x < w; x++, s += 4)
                        {
                            // B,G,R order
                            gray[off + x] = (float)(0.2126 * s[2] + 0.7152 * s[1] + 0.0722 * s[0]);
                        }
                    }
                }

                switch (mode)
                {
                    case DitherMode.OrderedBayer2: OrderedBayer(gray, w, h, Bayer2); break;
                    case DitherMode.OrderedBayer4: OrderedBayer(gray, w, h, Bayer4); break;
                    case DitherMode.OrderedBayer8: OrderedBayer(gray, w, h, Bayer8); break;
                    case DitherMode.FloydSteinberg: ErrorDiffuse(gray, w, h, FS); break;
                    case DitherMode.Atkinson: ErrorDiffuse(gray, w, h, Atkinson); break;
                    default: QuantizeInPlace(gray); break;
                }

                byte[] packed = new byte[(w * h + 3) / 4];
                int pIdx = 0, shift = 6; byte pByte = 0;

                unsafe
                {
                    byte* dstBase = (byte*)dstData.Scan0.ToPointer();

                    for (int y = 0; y < h; y++)
                    {
                        byte* d = dstBase + y * dstStride;

                        for (int x = 0; x < w; x++)
                        {
                            byte idx = (byte)gray[y * w + x]; // already 0..3
                            d[x] = idx;

                            pByte |= (byte)(idx << shift);
                            if (shift == 0) { packed[pIdx++] = pByte; pByte = 0; shift = 6; }
                            else shift -= 2;
                        }
                    }
                }
                if (shift != 6) packed[pIdx] = pByte;

                return new TwoBppResult { DisplayBitmap = disp, Packed2Bpp = packed };
            }
            finally
            {
                if (srcData != null) src32.UnlockBits(srcData);
                if (dstData != null) disp.UnlockBits(dstData);
                src32.Dispose(); // we created the clone, so we dispose it
            }
        }

        // --------- Dithering helpers ---------

        private static void OrderedBayer(float[] g, int w, int h, int[,] bayer)
        {
            int n = bayer.GetLength(0);
            float invN2 = 1f / (n * n);
            float step = 255f / 4f; // 4 levels

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = g[y * w + x];
                    float t = (float)(((bayer[y % n, x % n] + 0.5) * invN2 - 0.5) * step);
                    int idx = Clamp01To3((int)Math.Round((v + t) / 85.0));
                    g[y * w + x] = idx;
                }
        }

        private static void ErrorDiffuse(float[] g, int w, int h, KernelWeight[] kernel)
        {
            for (int y = 0; y < h; y++)
            {
                bool lr = (y % 2 == 0); // serpentine
                int xStart = lr ? 0 : w - 1, xEnd = lr ? w : -1, inc = lr ? 1 : -1;

                for (int x = xStart; x != xEnd; x += inc)
                {
                    int i = y * w + x;
                    float v = g[i];
                    int idx = Clamp01To3((int)Math.Round(v / 85.0));
                    float q = Shades[idx];
                    float err = v - q;
                    g[i] = idx; // store palette index

                    for (int k = 0; k < kernel.Length; k++)
                    {
                        int dx = lr ? kernel[k].Dx : -kernel[k].Dx;
                        int nx = x + dx;
                        int ny = y + kernel[k].Dy;
                        if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;
                        int ni = ny * w + nx;
                        float nv = g[ni] + err * kernel[k].W;
                        if (nv < 0) nv = 0; else if (nv > 255) nv = 255;
                        g[ni] = nv;
                    }
                }
            }
        }

        private static void QuantizeInPlace(float[] g)
        {
            for (int i = 0; i < g.Length; i++)
                g[i] = Clamp01To3((int)Math.Round(g[i] / 85.0));
        }

        private static int Clamp01To3(int v) { return v < 0 ? 0 : (v > 3 ? 3 : v); }

        // Palettes / matrices / kernels

        private static void SetFourShadePalette(Bitmap bmp)
        {
            Color c0 = Color.FromArgb(255, Shades[0], Shades[0], Shades[0]);
            Color c1 = Color.FromArgb(255, Shades[1], Shades[1], Shades[1]);
            Color c2 = Color.FromArgb(255, Shades[2], Shades[2], Shades[2]);
            Color c3 = Color.FromArgb(255, Shades[3], Shades[3], Shades[3]);

            ColorPalette pal = bmp.Palette;
            pal.Entries[0] = c0; pal.Entries[1] = c1; pal.Entries[2] = c2; pal.Entries[3] = c3;
            for (int i = 4; i < pal.Entries.Length; i++) pal.Entries[i] = c3;
            bmp.Palette = pal;
        }

        private static readonly int[,] Bayer2 = {
        {0,2},
        {3,1}
    };

        private static readonly int[,] Bayer4 = {
        { 0,  8,  2, 10},
        {12,  4, 14,  6},
        { 3, 11,  1,  9},
        {15,  7, 13,  5}
    };

        private static readonly int[,] Bayer8 = {
        { 0,32, 8,40, 2,34,10,42},
        {48,16,56,24,50,18,58,26},
        {12,44, 4,36,14,46, 6,38},
        {60,28,52,20,62,30,54,22},
        { 3,35,11,43, 1,33, 9,41},
        {51,19,59,27,49,17,57,25},
        {15,47, 7,39,13,45, 5,37},
        {63,31,55,23,61,29,53,21}
    };

        private static readonly KernelWeight[] FS = {
        new KernelWeight(+1,0,7f/16f),
        new KernelWeight(-1,1,3f/16f), new KernelWeight(0,1,5f/16f), new KernelWeight(+1,1,1f/16f)
    };

        private static readonly KernelWeight[] Atkinson = {
        new KernelWeight(+1,0,1f/8f), new KernelWeight(+2,0,1f/8f),
        new KernelWeight(-1,1,1f/8f), new KernelWeight(0,1,1f/8f), new KernelWeight(+1,1,1f/8f),
        new KernelWeight(0,2,1f/8f)
    };
    }
}
