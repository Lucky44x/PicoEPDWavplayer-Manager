using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Manager
{
    public sealed class ImageDBReader : IDisposable
    {
        public const int Width = 120;
        public const int Height = 120;
        public const int BytesPerImage = (Width * Height) / 4; // 2bpp -> 4 pixels per byte
        private readonly FileStream _fs;

        public ImageDBReader(string path)
        {
            _fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (_fs.Length % BytesPerImage != 0)
                throw new InvalidDataException($"images.db length {_fs.Length} is not a multiple of {BytesPerImage}.");
        }

        public int ImageCount => (int)(_fs.Length / BytesPerImage);

        public byte[] ReadRawImageBytes(int index)
        {
            if ((uint)index >= (uint)ImageCount) throw new ArgumentOutOfRangeException(nameof(index));
            var buf = new byte[BytesPerImage];
            _fs.Position = (long)index * BytesPerImage;
            int read = _fs.Read(buf, 0, buf.Length);
            if (read != buf.Length) throw new EndOfStreamException();
            return buf;
        }

        public static Bitmap Decode2BppToBitmap(byte[] src)
        {
            if (src == null || src.Length != BytesPerImage)
                throw new ArgumentException($"Expected {BytesPerImage} bytes", nameof(src));

            // We’ll write to a 32bpp ARGB for simplicity/compatibility.
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, Width, Height);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            try
            {
                // Map 2-bit value to grayscale intensity
                // (You can tweak this LUT if you want a different gamma)
                byte[] lut = { 0, 85, 170, 255 };

                int stride = data.Stride;
                IntPtr scan0 = data.Scan0;
                unsafe
                {
                    byte* dstBase = (byte*)scan0.ToPointer();

                    for (int y = 0; y < Height; y++)
                    {
                        byte* row = dstBase + y * stride;
                        int rowPixelOffset = y * Width;

                        for (int x = 0; x < Width; x++)
                        {
                            int pixelIndex = rowPixelOffset + x;
                            int byteIndex = pixelIndex >> 2;           // /4
                            int pairInByte = pixelIndex & 3;           // 0..3
                            int shift = 6 - (pairInByte * 2);          // MSB-first pairs
                            int val2bpp = (src[byteIndex] >> shift) & 0x3;
                            byte g = lut[val2bpp];

                            // Write BGRA
                            int pxOffset = x * 4;
                            row[pxOffset + 0] = g;    // B
                            row[pxOffset + 1] = g;    // G
                            row[pxOffset + 2] = g;    // R
                            row[pxOffset + 3] = 255;  // A
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp;
        }

        public void Dispose() => _fs?.Dispose();
    }
}
