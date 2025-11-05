# PICO-EPD-WAV-PLAYER

**This Project does nothing standalone and requires [My PICO-EPD-WAV Player](https://github.com/Lucky44x/PicoEPDAudioPlayer) to actually read the data that this project produces**  

### Explanation
This Project is aimed to manage, build and flash my custom Media-Library format, onto a micro-SD-Card which can then be used with my PICO-EPD-WAV Player to actually play the music stored on the micro-sd-card   

The Datastructure of the resulting micro-sd-card is meant to be highly space efficient utilising bitpacking in order to pack as much information about a song, artist and album into the .db files as **necessary**  
Thats right, not as much as possible, but as much as necessary:
- Names are truncated to 22 characters, since the display of my EPD cannot contain more
- Images are only saved in 2bpp grayscale in sizes of 120x120 since that is the only size I use on my display
- Databases use a lookup header with fixed header-sizes to guarantee O(1) lookups

Now, this may seem overkill for an micro-SD-card, considering those things can nowadays store more than 16GB in most cases...  
But originally I was planning to run the project on the flash-memory of the pico instead of an external storage medium, until I realized how ridicolous it was to try and save mp3 files on a 2M flash-memory chip, so here we are  

### Structures

#### Song Database (songs.db)
**File-Header** (0 bytes)  
└── **Song-Entry**[] _66 bytes per entry_  
&emsp;&emsp;├── md5-hash _16 bytes_  
&emsp;&emsp;├── song-name _44 bytes (2 bytes x 22 chars)_  
&emsp;&emsp;├── artist-id _3 bytes (u24le - key in_ **artists.db**_)_  
&emsp;&emsp;└── image-id _3 bytes (u24le - key in_ **images.db**_)_  

#### Artist Database (artists.db)
**File-Header** (0 bytes)  
└── **Artist-Entry**[] _50 bytes per entry_  
&emsp;&emsp;├── artist-name _44 bytes (2 bytes x 22 chars)_  
&emsp;&emsp;├── album-id _3 bytes (u24le - key in_ **albums.db**_)_  
&emsp;&emsp;└── image-id _3 bytes (u24le - key in_ **images.db**_)_  

#### Albums Database (albums.db)
**File-Header** (3 bytes)  
├── album-count _3 bytes (u24le)_  
│  
├── **Album-Entry** [] _53 bytes per entry_  
│   ├── album-name _44 bytes (2 bytes x 22 chars)_  
│   ├── image-id _3 bytes (u24le - key in_ **images.db**_)_  
│   ├── song-count _3 bytes (u24le)_  
│   └── song-offset _3 bytes (u24le; in-file byte-offset (from 0x00) to the first song in this album)_  
│  
└── **Song-ID** [] _3 bytes per ID_  
    └── song-id _3 bytes (u24le; key in_ **songs.db**_)_  
  
#### Images Database (images.db)
File-Header (0 bytes)  
└── **Image**[] _(3600 bytes per Image -> (120px * 120px * 2 (2bpp)) / 8)_  
