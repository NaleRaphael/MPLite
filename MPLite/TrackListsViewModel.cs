using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
{
    using Playlist = Core.Playlist;

    public class TrackListsViewModel
    {
        private ObservableCollection<Playlist> trackLists = new ObservableCollection<Playlist>();

        public ObservableCollection<Playlist> TrackLists
        {
            get { return trackLists; }
            set { trackLists = value; }
        }

        public void UpdateTrackLists(List<Playlist> source)
        {
            trackLists.FillContent(source);
        }
    }
}
