using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MP3Manager
{
    internal class mp3_Album
    {
        private string name;
        private string TruncatedName;
        private mp3_Artist artist;
        private mp3_Image cover;
        private List<mp3_Song> songs;

        public mp3_Album(string name)
        {
            setName(name);
            this.songs = new List<mp3_Song>();
        }

        public string getName() { return name; }
        public string getTruncatedName() {  return TruncatedName; }
        
        public mp3_Artist getArtist() { return artist; }
        public mp3_Image getCover() { return cover; }
        public mp3_Song getSong(int idx) { return songs[idx]; }
        public mp3_Song[] getSongs() { return songs.ToArray(); }

        public void insertSong(int idx, mp3_Song song) 
        {
            if (idx == -1) songs.Add(song);
            else songs.Insert(idx, song);
        }

        public void removeSong(int idx) { songs.RemoveAt(idx); }
        public void removeSong(mp3_Song inst) { songs.Remove(inst); }

        public void setName(string name) 
        { 
            if (name == null) name = "Unknown Album";
            this.name = name;
            if (this.name.Length > 22)
            {
                this.TruncatedName = name.Substring(0, 19);
                TruncatedName += "...";
            }
            else this.TruncatedName = name;
        }

        public void setArtist(mp3_Artist artist) { this.artist = artist; }
        public void setCover(mp3_Image cover) { this.cover = cover; }
    }
}
