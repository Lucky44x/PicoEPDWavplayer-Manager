using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace MP3Manager
{
    internal class mp3_Image
    {
        private static readonly byte[] Shades = { 0, 85, 170, 255 };

        private string name;
        private string sourceHash;
        private string sourceFile;

        private bool generated = false;

        private DitherMode mode;

        private Image sourceImage;
        private Bitmap convertedImage;

        private byte[] packedBitmap;

        public mp3_Image(string name, Image source, bool isGenerated = false)
        {
            setName(name);
            this.sourceImage = source;
            this.generated = isGenerated;
            generateHash();
            generateGrayscale();
        }

        public mp3_Image(byte[] source)
        {
            // First set source Image
            sourceImage = ImageDBReader.Decode2BppToBitmap(source);
            generateHash();
            setName(sourceHash);
            generated = true;
            generateGrayscale();
        }

        private void generateHash()
        {
            using (var md5 = MD5.Create())
            {
                using (var ms = new MemoryStream())
                {
                    sourceImage.Save(ms, ImageFormat.Png);
                    ms.Position = 0;

                    byte[] byteHash = md5.ComputeHash(ms);
                    this.sourceHash = BitConverter.ToString(byteHash);
                }
            }
        }

        public bool isGenerated() { return generated; }
        public string getName() { return name; }
        public string getSourceHash() { return sourceHash; }
        public string getSourceFile() { return generated ? "Generated internally" : sourceFile; }
        public Image getSourceImage() { return sourceImage; }
        public Image getConvertedImage() { return convertedImage; }

        public byte[] getPackedBitmap() { return packedBitmap; }

        public void setName(string name) { this.name = name; }

        public void setDitherMode(DitherMode mode) { this.mode = mode; }
        public DitherMode getDitherMode() { return mode; }

        public void generateGrayscale()
        {
            var result = ImageGrayscaleConverter.Convert(new Bitmap(sourceImage), this.mode);
            this.packedBitmap = result.Packed2Bpp;
            this.convertedImage = result.DisplayBitmap;
        }
    }
}
