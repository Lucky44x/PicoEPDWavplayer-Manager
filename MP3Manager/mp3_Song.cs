using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TagLib.Mpeg;

namespace MP3Manager
{
    internal class mp3_Song
    {
        private string SongName;
        private string TruncatedSongName;
        private string SourceFile;
        private string fileType;
        private string length;
        private string hash;

        private byte[] byteHash;

        private mp3_Artist artist;
        private mp3_Image image;

        public mp3_Song(string sourceFile)
        {
            TagLib.File file = TagLib.File.Create(sourceFile);

            setName(file.Tag.Title);
            //this.SongName = name;
            this.SourceFile = sourceFile;

            this.length = file.Properties.Duration.ToString();
            this.fileType = file.Properties.Codecs.First().Description;

            using (var sha = MD5.Create())
            {
                using (FileStream stream = System.IO.File.OpenRead(this.SourceFile))
                {
                    byteHash = sha.ComputeHash(stream);
                    this.hash = BitConverter.ToString(byteHash);
                }
            }
        }

        public string getName() { return this.SongName; }
        public string getTruncatedName() {  return this.TruncatedSongName; }

        public string getFilePath() { return this.SourceFile; }
        public string getFileType() { return this.fileType; }
        public string getLength() { return this.length; }
        public string getHash() { return this.hash; }
        public string getForm() { return this.fileType; }

        public mp3_Artist getArtist() {  return this.artist; }
        public mp3_Image getImage() { return this.image; }

        public byte[] getByteHash() { return this.byteHash; }

        public void setArtist(mp3_Artist artist)
        {
            this.artist = artist;
        }

        public void setImage(mp3_Image image)
        {
            this.image = image;
        }

        public void setName(string name) 
        { 
            this.SongName = name;
            if (this.SongName.Length > 22)
            {
                this.TruncatedSongName = SongName.Substring(0, 19);
                TruncatedSongName += "...";
            }
            else this.TruncatedSongName = SongName;
        }
    }
}
