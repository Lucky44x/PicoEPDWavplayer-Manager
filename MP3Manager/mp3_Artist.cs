using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Manager
{
    internal class mp3_Artist
    {
        private string Name;

        private string TruncatedName;
        private mp3_Image image;

        public mp3_Artist(string name)
        {
            setName(name);
        }

        public string getName() { return Name; }
        public string getTruncatedName() { return TruncatedName; }

        public void setName(string name)
        {
            this.Name = name;
            if (this.Name.Length > 22)
            {
                this.TruncatedName = Name.Substring(0, 19);
                TruncatedName += "...";
            }
            else this.TruncatedName = Name;
        }

        public mp3_Image getImage() { return image; }

        public void setImage(mp3_Image image) { this.image = image; }
    }
}
