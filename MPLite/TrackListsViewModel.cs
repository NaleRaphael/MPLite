using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
{
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;

    public class TrackListsViewModel
    {
        private ObservableCollection<Playlist> trackLists = new ObservableCollection<Playlist>();

        public Playlist CurrentPlaylist { get; set; }

        public ObservableCollection<Playlist> TrackLists
        {
            get { return trackLists; }
            set { trackLists = value; }
        }

        public void UpdateTrackList(Playlist source)
        {
            trackLists.First(x => x.GUID == source.GUID).Soundtracks = source.Soundtracks;
        }

        public void UpdateTrackLists(List<Playlist> source)
        {
            trackLists.FillContent(source);
        }

        public Playlist CreatePlaylist()
        {
            string listName = PlaylistCollection.AddSerialNum(this.trackLists.ToList(), "New Playlist");
            Playlist pl = new Playlist(listName);
            PlaylistCollection.AddPlaylist(pl);
            trackLists.Add(pl);
            CurrentPlaylist = pl;
            return pl;
        }

        public void RemovePlaylist(Guid targetGUID)
        {
            PlaylistCollection.RemovePlaylist(targetGUID);
            trackLists.Remove(trackLists.First(x => x.GUID == targetGUID));
            CurrentPlaylist = (trackLists.Count == 0) ? null : trackLists[trackLists.Count - 1];
        }
    }
}
