using NAudio.Wave;
using SpotifyExplode.Albums;
using SpotifyExplode.Artists;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TagLib;

namespace MP3Manager
{
    public partial class Form1 : Form
    {
        private List<mp3_Song> songList = new List<mp3_Song>();
        private List<mp3_Album> albumList = new List<mp3_Album>();
        private List<mp3_Artist> artistList = new List<mp3_Artist>();
        private List<mp3_Image> imageList = new List<mp3_Image>();

        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(finishDatabaseFlash);
            backgroundWorker1.DoWork += new DoWorkEventHandler(databaseBuild_Worker);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(updateDatabaseProgress);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            imagePropDitherBox.DataSource = Enum.GetValues(typeof(DitherMode));
            internal_refresh_drive_list();
        }

        private void imagePage_Click(object sender, EventArgs e) { }
        private void albumPage_Click(object sender, EventArgs e) { }
        private void albumPropPanel_Paint(object sender, PaintEventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }

        #region Song

        private void internal_add_song(mp3_Song song)
        {
            TagLib.File file = TagLib.File.Create(song.getFilePath());

            //Get Artist
            int artistIdx = internal_find_artist(file.Tag.FirstPerformer);
            if (artistIdx > -1) song.setArtist(artistList[artistIdx]);

            int imageIdx = internal_try_generate_image(file);
            if (imageIdx > -1) song.setImage(imageList[imageIdx]);

            songList.Add(song);
            
            int albumIdx = internal_find_album(file.Tag.Album, file);
            if (albumIdx > -1) albumList[albumIdx].insertSong(-1, song);

            songBox.Items.Add(song.getName());
            songBox.SelectedIndex = songBox.Items.Count - 1;

            //internal_refresh_song_metadata(song);
        }

        private void internal_refresh_song_metadata(mp3_Song song)
        {
            songMetaFile.Text = "File: " + song.getFilePath();
            songMetaLen.Text = "Length: " + song.getLength();
            songMetaHash.Text = "Hash: " + song.getHash();
            songMetaForm.Text = "Format: " + song.getForm();
            songPropNameBox.Text = song.getName();
            songPropArtistBox.SelectedIndex = artistList.IndexOf(song.getArtist());
            songMetaType.Text = song.getFilePath().EndsWith(".mp3") ? "MP3" : "WAV";

            songMetaTruncatedName.Text = "Trunc: " + song.getTruncatedName();

            if (song.getImage() != null)
            {
                if (song.getImage().getSourceImage() != null)
                    songPropPictureBox.Image = song.getImage().getSourceImage();

                if (song.getImage().getSourceImage() != null)
                    songPropConvPictureBox.Image = song.getImage().getConvertedImage();
            }
            else
            {
                songPropPictureBox.Image = null;
                songPropConvPictureBox.Image = null;
            }

            songPropImageBox.SelectedIndex = imageList.IndexOf(song.getImage());
        }

        private void internal_refresh_song_box()
        {
            songBox.Items.Clear();
            foreach (mp3_Song song in songList)
            {
                songBox.Items.Add(song.getName());
            }
        }

        private void addSongButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                openFileDialog1.InitialDirectory = "c:\\";
                ofd.Filter = "mp3 files (*.mp3)|*.mp3|wav files (*.wav)|*.wav|All Files (*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.Multiselect = true;
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach(string fileName in ofd.FileNames)
                    {
                        internal_add_song(new mp3_Song(fileName));
                    }
                }
                return;
            }
        }

        private void addSongWebButton_Click(object sender, EventArgs e)
        {
            string url = "";
            DialogResult result = InputBox("Web Adress", "Add Song via Web Adress (Spotify or Direct link)", ref url);
            if (result != DialogResult.OK) return;

            //Run Web-Download 
        }

        private void songBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            mp3_Song selectedSong = songList[songBox.SelectedIndex];
            if (selectedSong == null) if (MessageBox.Show("Error While Reading Song Info - Indexed at " + songBox.SelectedIndex + " - only " + songList.Count + " - Len available") == DialogResult.OK) return;

            internal_refresh_song_metadata(selectedSong);
        }

        private void songPropArtistBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            mp3_Song selectedSong = songList[songBox.SelectedIndex];
            if (selectedSong == null) if (MessageBox.Show("Error While Reading Song Info - Indexed at " + songBox.SelectedIndex + " - only " + songList.Count + " - Len available") == DialogResult.OK) return;
            if (artistList.Count <= 0 || songPropArtistBox.SelectedIndex < 0 || songPropArtistBox.SelectedIndex >= artistList.Count) return;

            selectedSong.setArtist(artistList[songPropArtistBox.SelectedIndex]);

            internal_refresh_song_metadata(selectedSong);
        }

        private void songPropImageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            mp3_Song selectedSong = songList[songBox.SelectedIndex];
            if (selectedSong == null) if (MessageBox.Show("Error While Reading Song Info - Indexed at " + songBox.SelectedIndex + " - only " + songList.Count + " - Len available") == DialogResult.OK) return;
            if (imageList.Count <= 0 || songPropImageBox.SelectedIndex < 0 || songPropImageBox.SelectedIndex >= imageList.Count) return;

            selectedSong.setImage(imageList[songPropImageBox.SelectedIndex]);

            internal_refresh_song_metadata(selectedSong);
        }

        private void songPropNameBox_TextChanged(object sender, EventArgs e)
        {
            int selIndex = songBox.SelectedIndex;
            mp3_Song selectedSong = songList[songBox.SelectedIndex];
            if (selectedSong == null) return;

            selectedSong.setName(songPropNameBox.Text);

            internal_refresh_song_box();
            songBox.SelectedIndex = selIndex;
        }
        #endregion

        #region Artist

        private void internal_refresh_artist_boxes()
        {
            songPropArtistBox.Items.Clear();
            albumPropArtistBox.Items.Clear();
            foreach (mp3_Artist artist in artistList)
            {
                songPropArtistBox.Items.Add(artist.getName());
                albumPropArtistBox.Items.Add(artist.getName());
            }
        }

        private void internal_add_artist(mp3_Artist artist)
        {
            artistList.Add(artist);
            artistBox.Items.Add(artist.getName());
            artistBox.SelectedIndex = artistBox.Items.Count - 1;

            internal_refresh_artist_boxes();
        }

        private void internal_refresh_artist_metadata(mp3_Artist artist)
        {
            artistPropNameBox.Text = artist.getName();

            if (artist.getImage() != null)
            {
                artistPropPictureBox.Image = artist.getImage().getSourceImage();
                artistPropConvPictureBox.Image = artist.getImage().getConvertedImage();
                artistPropImageBox.SelectedIndex = imageList.IndexOf(artist.getImage());
            }
            else
            {
                artistPropImageBox.SelectedIndex = -1;
                artistPropPictureBox.Image = null;
                artistPropConvPictureBox.Image = null;
            }

            artistMetaTruncatedNameLabel.Text = "Trunc: " + artist.getTruncatedName();
        }

        private void internal_refresh_artist_box()
        {
            artistBox.Items.Clear();
            foreach (mp3_Artist artist in artistList)
            {
                artistBox.Items.Add(artist.getName());
            }
        }

        private int internal_find_artist(string name)
        {
            int foundIndex = -1;
            foreach (mp3_Artist artist in artistList)
            {
                if (artist.getName().ToUpper().Equals(name.ToUpper()))
                {
                    foundIndex = artistList.IndexOf(artist);
                    break;
                }
            }

            if (foundIndex != -1) return foundIndex;

            internal_add_artist(new mp3_Artist(name));
            foundIndex = artistList.Count - 1;
            return foundIndex;
        }

        private void artistBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            mp3_Artist selectedArtist = artistList[artistBox.SelectedIndex];
            if (selectedArtist == null) return;

            internal_refresh_artist_metadata(selectedArtist);
        }

        private void artistPropNameBox_TextChanged(object sender, EventArgs e)
        {
            int selIndex = artistBox.SelectedIndex;
            mp3_Artist selectedArtist = artistList[artistBox.SelectedIndex];
            if (selectedArtist == null) return;

            selectedArtist.setName(artistPropNameBox.Text);

            internal_refresh_artist_box();
            internal_refresh_artist_boxes();
            artistBox.SelectedIndex = selIndex;
        }

        private void artistPropImageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selIndex = artistBox.SelectedIndex;
            mp3_Artist selectedArtist = artistList[artistBox.SelectedIndex];
            if (selectedArtist == null) return;

            int selImgIdx = artistPropImageBox.SelectedIndex;
            if (selImgIdx < 0 || selIndex >= imageList.Count) return;

            selectedArtist.setImage(imageList[selImgIdx]);

            internal_refresh_artist_metadata(selectedArtist);
        }

        private void addArtistButton_Click(object sender, EventArgs e) {}
        private void artistPropNameBox_KeyPress(object sender, KeyPressEventArgs e) {}

        #endregion

        #region Images
        private void internal_add_image(mp3_Image image)
        {
            imageList.Add(image);
            internal_refresh_image_box();
            imageBox.SelectedIndex = imageBox.Items.Count - 1;
        }

        private void internal_refresh_image_metadata(mp3_Image image)
        {
            imagePropNameBox.Text = image.getName();
            imagePropFile.Text = "File: " + image.getSourceFile();
            imagePropHash.Text = "Hash: " + image.getSourceHash();
            imagePropPictureBox.Image = image.getSourceImage();
            imagePropConvPictureBox.Image = image.getConvertedImage();
        }

        private void internal_refresh_image_box()
        {
            imageBox.Items.Clear();
            songPropImageBox.Items.Clear();
            artistPropImageBox.Items.Clear();
            albumPropImageBox.Items.Clear();
            
            foreach (mp3_Image image in imageList)
            {
                imageBox.Items.Add(image.getName());
                songPropImageBox.Items.Add(image.getName());
                albumPropImageBox.Items.Add(image.getName());
                artistPropImageBox.Items.Add(image.getName());
            }
        }

        private int internal_find_image(Image image, string name = null, bool generated = false)
        {
            var localHash = "";
            using (var md5 = MD5.Create())
            {
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    ms.Position = 0;

                    byte[] byteHash = md5.ComputeHash(ms);
                    localHash = BitConverter.ToString(byteHash);
                }
            }

            foreach(mp3_Image im in imageList)
            {
                if (im.getSourceHash() == localHash) return imageList.IndexOf(im);
            }

            //Fallback to creating new image
            string imgName = name == null ? "" + imageList.Count : name;
            internal_add_image(new mp3_Image(imgName, image, generated));
            return imageList.Count - 1;
        }

        private int internal_try_generate_image(TagLib.File file)
        {
            //Get Cover Art
            int pictureIndex = 0;
            while (pictureIndex < file.Tag.Pictures.Length)
            {
                try
                {
                    IPicture albumCover = file.Tag.Pictures[pictureIndex];
                    ImageConverter converter = new ImageConverter();
                    Image coverArt = converter.ConvertFrom(albumCover.Data.Data) as Image;
                    return internal_find_image(coverArt.GetThumbnailImage(120, 120, null, IntPtr.Zero), file.Tag.Album, true);
                }
                catch (Exception ex)
                {
                    pictureIndex++;
                }

                break;
            }

            return -1;
        }

        private void imageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            mp3_Image selectedImage = imageList[imageBox.SelectedIndex];
            if (selectedImage == null) return;

            internal_refresh_image_metadata(selectedImage);
        }

        private void imagePropDitherBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (imageList.Count == 0) return;

            mp3_Image selectedImage = imageList[imageBox.SelectedIndex];
            if (selectedImage == null) return;

            selectedImage.setDitherMode((DitherMode)imagePropDitherBox.SelectedIndex);
            selectedImage.generateGrayscale();
            internal_refresh_image_metadata(selectedImage);
        }

        private void imageAddButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                openFileDialog1.InitialDirectory = "c:\\";
                ofd.Filter = "image files (*.png; *.jpg)|*.png;*.jpg|All Files (*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.Multiselect = false;
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    mp3_Image image = new mp3_Image(filePath.Split('\\').Last().Split('.').First(), Image.FromFile(filePath).GetThumbnailImage(120, 120, null, IntPtr.Zero));
                    internal_add_image(image);
                }
                return;
            }
        }

        private void imagePropNameBox_TextChanged(object sender, EventArgs e)
        {
            int selIndex = imageBox.SelectedIndex;
            mp3_Image selectedImage = imageList[imageBox.SelectedIndex];
            if (selectedImage == null) return;

            selectedImage.setName(imagePropNameBox.Text);

            internal_refresh_image_box();
            imageBox.SelectedIndex = selIndex;
        }

        #endregion

        #region Albums

        private void internal_add_album(mp3_Album album, bool silent = false)
        {
            albumList.Add(album);
            if (!silent) internal_refresh_album_box();
        }

        private void internal_refresh_album_metadata(mp3_Album album)
        {
            albumPropNameBox.Text = album.getName();

            if (album.getCover() != null)
            {
                albumPropImageBox.SelectedIndex = imageList.IndexOf(album.getCover());
                albumPropPictureBox.Image = album.getCover().getSourceImage();
                albumPropConvPictureBox.Image = album.getCover().getConvertedImage();
            }
            else
            {
                albumPropImageBox.SelectedIndex = -1;
                albumPropPictureBox.Image = null;
                albumPropConvPictureBox.Image = null;
            }

            albumPropArtistBox.SelectedIndex = artistList.IndexOf(album.getArtist());
            albumTruncatedNameLabel.Text = "Trunc: " + album.getTruncatedName();

            internal_refresh_album_song_list(album);
        }

        private void internal_refresh_album_song_list(mp3_Album album)
        {
            albumSongList.Items.Clear();
            foreach(mp3_Song song in album.getSongs())
            {
                if (song == null) albumSongList.Items.Add("Unknown");
                else albumSongList.Items.Add(song.getName());
            }
        }

        private void internal_refresh_album_box()
        {
            albumBox.Items.Clear();
            foreach (mp3_Album album in albumList)
            {
                albumBox.Items.Add(album.getName());
            }
        }

        private int internal_find_album(string albumName, TagLib.File mp3File = null)
        {
            foreach (mp3_Album album in albumList)
            {
                if (albumName.ToLower().Equals(album.getName().ToLower())) return albumList.IndexOf(album);
            }

            mp3_Album newAlbum = new mp3_Album(albumName);
            
            if (mp3File != null)
            {
                int albumCover = internal_try_generate_image(mp3File);
                if (albumCover != -1) newAlbum.setCover(imageList[albumCover]);

                int artistId = internal_find_artist(mp3File.Tag.FirstPerformer);
                if (artistId != -1) newAlbum.setArtist(artistList[artistId]);
            }

            internal_add_album(newAlbum, backgroundWorker1.IsBusy);
            return albumList.Count - 1;
        }

        private void albumBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selIdx = albumBox.SelectedIndex;
            if (selIdx < 0) return;

            mp3_Album album = albumList[selIdx];

            internal_refresh_album_metadata(album);
        }

        private void albumSongList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selIdx = albumBox.SelectedIndex;
            if (selIdx < 0) return;

            mp3_Album album = albumList[selIdx];

            int selSongIdx = albumSongList.SelectedIndex;
            if (selSongIdx < 0)
            {
                albumSongMoveDown.Enabled = false;
                albumSongMoveUp.Enabled = false;
                albumSongRemove.Enabled = false;
                return;
            }

            mp3_Song song = album.getSong(selSongIdx);
            if (selSongIdx > 0)
                albumSongMoveUp.Enabled = true;
            else albumSongMoveUp.Enabled = false;

            if (selSongIdx < album.getSongs().Count() - 1)
                albumSongMoveDown.Enabled = true;
            else albumSongMoveDown.Enabled = false;

            albumSongRemove.Enabled = true;

            if (song.getImage() != null)
            {
                albumSongPicture.Image = song.getImage().getSourceImage();
            }
            else
            {
                albumSongPicture.Image = null;
            }
        }

        private void albumSongList_Leave(object sender, EventArgs e)
        {
            /*
            albumSongMoveDown.Enabled = false;
            albumSongMoveUp.Enabled = false;
            albumSongRemove.Enabled = false;
            */
        }

        private void albumSongMoveDown_Click(object sender, EventArgs e)
        {
            int selIdx = albumBox.SelectedIndex;
            if (selIdx < 0) return;

            mp3_Album album = albumList[selIdx];

            int selSongIdx = albumSongList.SelectedIndex;
            if (selSongIdx < 0) return;

            mp3_Song song = album.getSong(selSongIdx);
            album.removeSong(selSongIdx);
            album.insertSong(selSongIdx + 1, song);

            internal_refresh_album_song_list(album);
            albumSongList.SelectedIndex = selSongIdx + 1;
        }

        private void albumSongMoveUp_Click(object sender, EventArgs e)
        {
            int selIdx = albumBox.SelectedIndex;
            if (selIdx < 0) return;

            mp3_Album album = albumList[selIdx];

            int selSongIdx = albumSongList.SelectedIndex;
            if (selSongIdx < 0) return;

            mp3_Song song = album.getSong(selSongIdx);
            album.removeSong(selSongIdx);
            album.insertSong(selSongIdx - 1, song);

            internal_refresh_album_song_list(album);
            albumSongList.SelectedIndex = selSongIdx - 1;
        }

        private void albumSongRemove_Click(object sender, EventArgs e)
        {
            int selIdx = albumBox.SelectedIndex;
            if (selIdx < 0) return;

            mp3_Album album = albumList[selIdx];

            int selSongIdx = albumSongList.SelectedIndex;
            if (selSongIdx < 0) return;

            mp3_Song song = album.getSong(selSongIdx);
            album.removeSong(selSongIdx);

            internal_refresh_album_metadata(album);
        }

        #endregion

        #region Overview
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            internal_refresh_overview_page();
        }

        private void internal_refresh_overview_page()
        {
            //Tally up songs
            overviewSongNumBox.Text = "" + songList.Count;
            overviewSongSizeBox.Text = "~ " + (songList.Count * 88) / 1000.0 + " kb";

            //Tally up songFiles
            overviewSongFileNumBox.Text = "" + songList.Count;
            long songFileSize = 0;
            foreach (mp3_Song song in songList)
            {
                songFileSize += new System.IO.FileInfo(song.getFilePath()).Length;
            }
            overviewSongFileSizeBox.Text = "~" + songFileSize / 1000000.0 + " mb";

            //Tally up Album
            overviewAlbumNumBox.Text = "" + albumList.Count;
            long albumSize = albumList.Count * 72;
            foreach (mp3_Album album in albumList)
            {
                albumSize += album.getSongs().Length * 3;
            }
            overviewAlbumSizeBox.Text = "~ " + albumSize / 1000.0 + " kb";

            //Tally up Artists
            overviewArtistNumBox.Text = "" + artistList.Count;
            overviewArtistsSizeBox.Text = "~ " + (artistList.Count * 72) / 1000.0 + " kb";

            //Tally up Images
            overViewImagesNumBox.Text =  "" + imageList.Count;
            overviewImagesSizeBox.Text = "~ " + (imageList.Count * 3600) / 1000.0 + " kb";

            overViewTotalSizeBox.Text = "~ " + (
                (songList.Count * 88)
                + (artistList.Count * 72)
                + (imageList.Count * 3600)
                + (albumSize)
                + (songFileSize)) / 1000000.0 + " mb";
        }

        private void internal_refresh_drive_list()
        {
            overviewDriveBox.Items.Clear();
            foreach(var drive in System.IO.DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed) continue;

                overviewDriveBox.Items.Add(drive.Name);
            }
        }

        private void overviewDiskRefreshButton_Click(object sender, EventArgs e)
        {
            internal_refresh_drive_list();
        }

        private void overviewFlashButton_Click(object sender, EventArgs e)
        {
            if(backgroundWorker1.IsBusy || progressForm != null)
            {
                MessageBox.Show("There is already an operation in progress", "Thread Occupied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string driveLetter = overviewDriveBox.SelectedItem as string;
            driveLetter = driveLetter.Substring(0, 1);

            /*
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/K format " + driveLetter + ": /FS:exFat /V:einkplayer /Q /Y",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
                Verb = "runas"
            };

            using (var proc = Process.Start(psi))
            {
                proc.WaitForExit();
            }
            */

            // Generate all song hashes
            List<string> songHashes = new List<string>();
            foreach(mp3_Song song in songList)
            {
                songHashes.Add(song.getHash().ToUpper());
            }

            // Clear unused songs
            string[] files = Directory.GetFiles(driveLetter + ":\\");
            foreach (String file in files)
            {
                if (file.EndsWith(".db"))
                {
                    System.IO.File.Delete(file);
                    continue;
                }

                string fileName = file.Substring(0, file.Length - 4);
                if (songHashes.Contains(fileName.ToUpper())) continue;  // Keep files with the same hash-id
                System.IO.File.Delete(fileName);                        // Delete all those that arent used
            }

            buildDatabases(driveLetter + ":\\");
        }

        private void overviewBuildButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select directory to build databases adn files into";
                fbd.ShowNewFolderButton = true;
                
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = fbd.SelectedPath;
                    buildDatabases(selectedPath);
                }
            }
        }

        private void overViewLoadButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select directory to build databases adn files into";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = fbd.SelectedPath;
                    MessageBox.Show("Load databases located in: " + selectedPath);
                }
            }
        }
        #endregion

        #region Builder
        private DatabaseFlasher progressForm = null;
        private string databasePath = null;
        private float totalFileCount = 0;
        private float currentFileCount = 0;

        private void databaseBuild_Worker(object sender, DoWorkEventArgs e)
        {
            //progressForm.change_flash_step("Songs");
            buildSongDatabase(databasePath);

            //progressForm.change_flash_step("Artists");
            buildArtistDatabase(databasePath);

            //progressForm.change_flash_step("Images");
            buildImageDatabase(databasePath);

            //progressForm.change_flash_step("Albums");
            buildAlbumDatabase(databasePath);
        }

        private void buildDatabases(string path)
        {
            if(databasePath != null || backgroundWorker1.IsBusy || progressForm != null)
            {
                MessageBox.Show("There is already a Flash running...");
                return;
            }

            totalFileCount = 0;
            totalFileCount += songList.Count;
            totalFileCount += imageList.Count;
            //TODO: Replace this with a proper search since artist could very well alreay have their artist-album
            totalFileCount += albumList.Count + artistList.Count;
            totalFileCount += artistList.Count;

            progressForm = new DatabaseFlasher();
            progressForm.Show();

            databasePath = path;
            backgroundWorker1.RunWorkerAsync();
        }

        private void updateDatabaseProgress(object sender, ProgressChangedEventArgs e)
        {
            progressForm.update_progress(e.ProgressPercentage);
        }

        private void finishDatabaseFlash(object sender, RunWorkerCompletedEventArgs e)
        {
            progressForm.flashing_completed();
            progressForm = null;
            MessageBox.Show("Flashing onto drive " + databasePath + " finished, for a total of " + totalFileCount.ToString() + " files", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
            databasePath = null;
        }

        private void buildImageDatabase(string path)
        {
            using (BinaryWriter bw = new BinaryWriter(System.IO.File.Create(Path.Combine(path, "images.db"))))
            {
                System.Console.WriteLine("Writing " + imageList.Count + " Images");

                foreach (mp3_Image image in imageList)
                {
                    Console.WriteLine("         Writing " + image.getPackedBitmap().Length + " bytes for image");
                    bw.Write(image.getPackedBitmap());

                    currentFileCount++;
                    backgroundWorker1.ReportProgress((int)((currentFileCount/totalFileCount) * 100));
                }
            }
        }

        private void buildAlbumDatabase(string path)
        {
            using (BinaryWriter bw = new BinaryWriter(System.IO.File.Create(Path.Combine(path, "albums.db"))))
            {
                Console.WriteLine("Writing " + albumList.Count + " Albums...");

                long headerPos = 0x00;
                long bodyPos = (long)(albumList.Count * 53 + 3);

                bw.WriteU24LE((uint)albumList.Count);

                foreach (mp3_Album album in albumList)
                {
                    byte[] albumName = ConvertUTF8_2Byte(album.getTruncatedName());
                    bw.Write(albumName);             //Write Artist Name

                    if (albumName.Length < 22 * 2)
                    {
                        byte[] padding = new byte[(22 * 2) - albumName.Length];
                        bw.Write(padding);
                    }

                    uint imageID = (uint)imageList.IndexOf(album.getCover());
                    bw.WriteU24LE(imageID);

                    uint count = (uint)album.getSongs().Length;
                    bw.WriteU24LE(count);

                    uint songListOffset = (uint)bodyPos;
                    bw.WriteU24LE(songListOffset);

                    headerPos = bw.BaseStream.Position;
                    bw.BaseStream.Position = bodyPos;
                    foreach (mp3_Song song in album.getSongs())
                    {
                        uint songID = (uint)songList.IndexOf(song);
                        bw.WriteU24LE(songID);
                        bodyPos += 3;
                    }
                    bw.BaseStream.Position = headerPos;

                    currentFileCount++;
                    backgroundWorker1.ReportProgress((int)((currentFileCount/totalFileCount) * 100));
                }
            }
        }

        private void buildArtistDatabase(string path)
        {
            using (BinaryWriter bw = new BinaryWriter(System.IO.File.Create(Path.Combine(path, "artists.db"))))
            {
                System.Console.WriteLine("Writing " + artistList.Count + " Artists");

                foreach (mp3_Artist artist in artistList)
                {
                    //Build Artist Playlist/Album
                    int albumIDx = internal_find_album(artist.getName());
                    mp3_Album artistAlbum = albumList[albumIDx];

                    artistAlbum.setCover(artist.getImage());
                    artistAlbum.setArtist(artist);
                    foreach (mp3_Song song in songList)
                    {
                        if (song.getArtist() != artist) continue;
                        artistAlbum.insertSong(-1, song); //Adds to back of list
                    }

                    byte[] artistName = ConvertUTF8_2Byte(artist.getTruncatedName());
                    bw.Write(artistName);             //Write Artist Name

                    if (artistName.Length < 22 * 2)
                    {
                        byte[] padding = new byte[(22 * 2) - artistName.Length];
                        bw.Write(padding);
                    }

                    uint imageID = (uint)imageList.IndexOf(artist.getImage());
                    bw.WriteU24LE(imageID);

                    uint albumID = (uint)albumIDx;
                    bw.WriteU24LE(albumID);

                    currentFileCount++;
                    backgroundWorker1.ReportProgress((int)((currentFileCount/totalFileCount) * 100));
                }
            }
        }

        private void buildSongDatabase(string path)
        {
            using (BinaryWriter bw = new BinaryWriter(System.IO.File.Create(Path.Combine(path, "songs.db"))))
            {
                System.Console.WriteLine("Writing " + songList.Count + " Songs");

                foreach (mp3_Song song in songList)
                {
                    //Write file
                    string fname = MD5ToLowerHexNoSep(song.getByteHash()) + ".wav";
                    //System.IO.File.Copy(song.getFilePath(),Path.Combine(path, fname), overwrite: false);

                    if (!System.IO.File.Exists(Path.Combine(path, fname)))
                    {
                        //Convert to wav if not already
                        string originalSongPath = song.getFilePath();

                        //TODO: Should work? Hopefully?
                        switch (originalSongPath.Split('.').Last().ToLower())
                        {
                            case "wav":
                                System.IO.File.Copy(song.getFilePath(), Path.Combine(path, fname), overwrite: false);
                                break;
                            case "mp3":
                                using (var reader = new MediaFoundationReader(originalSongPath))
                                {
                                    WaveFileWriter.CreateWaveFile16(Path.Combine(path, fname), reader.ToSampleProvider());
                                }
                                break;
                        }
                    }

                    //Write Header Data
                    bw.Write(song.getByteHash());   //Write Hash
                    
                    byte[] songName = ConvertUTF8_2Byte(song.getTruncatedName());
                    bw.Write(songName);             //Write Song Name
                    
                    if (songName.Length < 22 * 2)
                    {
                        byte[] padding = new byte[(22 * 2) - songName.Length];
                        bw.Write(padding);
                    }

                    uint artistID = (uint)artistList.IndexOf(song.getArtist());
                    bw.WriteU24LE(artistID);        //Write Linked Artist ID

                    uint imageID = (uint)imageList.IndexOf(song.getImage());
                    bw.WriteU24LE(imageID);         //Write Linked Image ID

                    currentFileCount++;
                    backgroundWorker1.ReportProgress((int)((currentFileCount/totalFileCount) * 100));
                }
            }
        }

        #endregion

        #region Util

        private string MD5ToLowerHexNoSep(byte[] md5)
        {
            StringBuilder sb = new StringBuilder(md5.Length * 2);
            foreach (byte b in md5)
                sb.Append(b.ToString("x2")); // lowercase hex, no hyphens

            return sb.ToString();
        }

        private byte[] ConvertUTF8_2Byte(string s)
        {
            if (s == null) s = "";

            //Prepare output
            byte[] result = new byte[s.Length * 2];
            int outIndex = 0;

            for (int i = 0; i < s.Length; i++)
            {
                int codePoint;

                // Handle surrogate pairs (for chars beyond BMP)
                if (char.IsHighSurrogate(s[i]))
                {
                    if (i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                    {
                        codePoint = 0xFFFD;
                        i++; // skip low surrogate
                    }
                    else
                    {
                        // Invalid surrogate, replace with U+FFFD
                        codePoint = 0xFFFD;
                    }
                }
                else
                {
                    codePoint = s[i];
                }

                result[outIndex++] = (byte)(codePoint & 0xFF);
                result[outIndex++] = (byte)((codePoint >> 8) & 0xFF);
            }

            return result;
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
        #endregion
    }

    public static class U24
    {
        public static void WriteU24LE(this BinaryWriter bw, uint value)
        {
            if (value > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(value));
            bw.Write((byte)(value & 0xFF));
            bw.Write((byte)((value >> 8) & 0xFF));
            bw.Write((byte)((value >> 16) & 0xFF));
        }

        public static void WriteU24BE(this BinaryWriter bw, uint value)
        {
            if (value > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(value));
            bw.Write((byte)((value >> 16) & 0xFF));
            bw.Write((byte)((value >> 8) & 0xFF));
            bw.Write((byte)(value & 0xFF));
        }
    }
}
