using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Itenso.Windows.Controls.ListViewLayout;
using CSCore;
using CSCore.SoundOut;
using CSCore.Codecs.MP3;
using System.Threading;

namespace MPLite
{
    public partial class PagePlaylist : Page
    {
        public delegate void PlayTrackEventHandler(TrackInfo trackInfo);
        public static event PlayTrackEventHandler PlayTrackEvent;

        // Workaround for avoid playing wrong song when there are duplicates
        private int prevTrackIdx = -1;
        private int currTrackIdx = -1;

        public PagePlaylist()
        {
            InitializeComponent();
            InitPlaylist();
            ListBox_Playlist.SelectedIndex = 0;  // select default list
            MainWindow.GetTrackEvent += MainWindow_GetTrackEvent;
            MainWindow.TrackIsPlayedEvent += MainWindow_TrackIsPlayingEvent;
            MainWindow.TrackIsStoppedEvent += MainWindow_TrackIsStoppedEvent;
        }

        private void MainWindow_TrackIsStoppedEvent(TrackInfo track)
        {
            //TrackInfo selectedTrack = LV_Playlist.Items.OfType<TrackInfo>().ToList().Find(x => x.TrackName == track.TrackName);
            TrackInfo selectedTrack = LV_Playlist.Items.OfType<TrackInfo>().ToList()[prevTrackIdx];
            selectedTrack.PlayingSign = "";
        }

        private void MainWindow_TrackIsPlayingEvent(TrackInfo track)
        {
            // try to get item according to track
            //TrackInfo selectedTrack = LV_Playlist.Items.OfType<TrackInfo>().ToList().Find(x => x.TrackName == track.TrackName);
            TrackInfo selectedTrack = LV_Playlist.Items.OfType<TrackInfo>().ToList()[currTrackIdx];
            selectedTrack.PlayingSign = ">";
        }

        private void InitPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            foreach (TrackInfo track in plc.TrackLists[0].Soundtracks)
            {
                LV_Playlist.Items.Add(track);
            }
        }

        private void LV_Playlist_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void LV_Playlist_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string selectedPlaylist = ((ListBoxItem)ListBox_Playlist.SelectedValue).Content.ToString();

            // Update LV_Playlist
            foreach (string filePath in files)
            {
                // TODO: rewrite this
                //       Store data into a dataset (json file)
                //       Then present those information into listview
                string trackName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                TrackInfo trackInfo = new TrackInfo { TrackName = trackName, TrackPath = filePath };
                LV_Playlist.Items.Add(trackInfo);
            }

            // Update database
            PlaylistCollection.Update(files, selectedPlaylist);
            //PlaylistCollection.UpdateDatabase(LV_Playlist.Items.OfType<TrackInfo>().ToList<TrackInfo>(), selectedPlaylist);
        }

        private void LV_Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int selIdx = LV_Playlist.SelectedIndex;
            if (selIdx < 0) return;
            prevTrackIdx = currTrackIdx;    // workaround
            currTrackIdx = selIdx;          // workaround
            PlaySoundtrack(selIdx);
        }

        private void LV_Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && LV_Playlist.SelectedItems.Count != 0)
            {
                string selectedPlaylist = ((ListBoxItem)ListBox_Playlist.SelectedValue).Content.ToString();
                List<int> selectedIdx = new List<int>();

                foreach (TrackInfo track in LV_Playlist.SelectedItems)
                {
                    selectedIdx.Add(LV_Playlist.Items.IndexOf(track));
                }

                selectedIdx.Sort((x, y) => { return -x.CompareTo(y); });
                foreach (int idx in selectedIdx)
                {
                    LV_Playlist.Items.RemoveAt(idx);
                }

                // UPDATE database
                PlaylistCollection.DeleteTracksByIndices(selectedIdx.ToArray<int>(), selectedPlaylist);
            }
        }

        private TrackInfo GetSoundtrack(int selIdx)
        {
            Playlist pl = PlaylistCollection.GetPlaylist(((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString());
            if (pl == null)
                throw new InvalidPlaylistException("Invalid playlist.");
            currTrackIdx = selIdx;      // workaround
            return pl.Soundtracks[selIdx];
        }

        private void PlaySoundtrack(int selIdx)
        {
            // Fire event to start playing track
            try
            {
                PlayTrackEvent(GetSoundtrack(selIdx));
                // Showing current playing track
                
            }
            catch
            {
                throw;
            }
        }

        private TrackInfo MainWindow_GetTrackEvent()
        {
            TrackInfo track;
            if (LV_Playlist.Items.Count == 0)
            {
                throw new EmptyPlaylistException("No tracks avalible");
            }

            int selIdx = (LV_Playlist.SelectedIndex > 0) ? LV_Playlist.SelectedIndex : 0;

            try
            {
                track = GetSoundtrack(selIdx);
            }
            catch
            {
                throw;
            }
            return track;
        }
    }

    public class EmptyPlaylistException : Exception
    {
        public EmptyPlaylistException(string message) : base(message)
        { }
    }
}
