using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
{
    public class PlaylistCollection
    {
        public List<Playlist> TrackLists { get; set; }

        public PlaylistCollection()
        {
            TrackLists = new List<Playlist>();
        }
    }

    public class Playlist
    {
        public string ListName { get; set; }
        public List<TrackInfo> Soundtracks { get; set; }

        public Playlist(string listName)
        {
            ListName = listName;
            Soundtracks = new List<TrackInfo>();
        }
    }
}
