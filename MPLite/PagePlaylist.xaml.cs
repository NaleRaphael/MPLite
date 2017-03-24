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

        private int idxOfPlayingTrack = -1;
        private string listHostingPlayingTrack = "";

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
            string listName = ((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString();
            
            // Check whether selected list is the one hosting the playing track.
            // If not, just reset idxOfPlayingTrack and listHostingPlayingTrack
            if (listName == listHostingPlayingTrack)
            {
                SetPlayingStateOfTrack(prevTrackIdx, "");
            }
            listHostingPlayingTrack = "";
            idxOfPlayingTrack = -1;
        }

        private void MainWindow_TrackIsPlayingEvent(TrackInfo track)
        {
            SetPlayingStateOfTrack(currTrackIdx, ">");
        }

        private void SetPlayingStateOfTrack(int trackIdx, string status)
        {
            TrackInfo selectedTrack = LV_Playlist.Items.OfType<TrackInfo>().ToList()[trackIdx];
            selectedTrack.PlayingSign = status;
        }

        private void InitPlaylist()
        {
            RefreshPlaylist();
            RefreshPlaylistContent(Properties.Settings.Default.LastSelectedPlaylist);
        }

        private void RefreshPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            foreach (Playlist pl in plc.TrackLists)
            {
                ListBoxItem lvi = new ListBoxItem();
                lvi.Content = pl.ListName;
                ListBox_Playlist.Items.Add(lvi);
            }
        }

        private void RefreshPlaylistContent(string selectedPlaylist)
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;
            Playlist pl = plc.TrackLists.Find(x => x.ListName == selectedPlaylist);

            LV_Playlist.Items.Clear();
            if (pl != null)
            {
                foreach (TrackInfo track in pl.Soundtracks)
                {
                    LV_Playlist.Items.Add(track);
                }
            }
            
            // Update info
            Properties.Settings.Default.LastSelectedPlaylist = selectedPlaylist;

            // Show playing track
            if ((ListBoxItem)ListBox_Playlist.SelectedItem != null)
            {
                string listName = ((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString();
                if (listName == listHostingPlayingTrack)
                {
                    SetPlayingStateOfTrack(currTrackIdx, ">");
                }
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
            //idxOfPlayingTrack = selIdx;
            PlaySoundtrack(currTrackIdx);
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
            //idxOfPlayingTrack = selIdx;
            return pl.Soundtracks[currTrackIdx];
        }

        private void PlaySoundtrack(int selIdx)
        {
            // Fire event to start playing track
            try
            {
                PlayTrackEvent(GetSoundtrack(selIdx));

                // Showing current playing track in status bar


                // Remember which track is playing
                prevTrackIdx = currTrackIdx;
                currTrackIdx = selIdx;
                listHostingPlayingTrack = ((ListBoxItem)(ListBox_Playlist.SelectedItem)).Content.ToString();
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

            idxOfPlayingTrack = (LV_Playlist.SelectedIndex > 0) ? LV_Playlist.SelectedIndex : 0;

            try
            {
                track = GetSoundtrack(idxOfPlayingTrack);
            }
            catch
            {
                throw;
            }
            return track;
        }

        private void btn_AddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = new ListBoxItem();
            string newListName = "New Playlist";  // TODO: add serial number

            // Add to database
            string listNameWithSerialNum = PlaylistCollection.AddPlaylist(newListName);

            // Add to ListBox
            lbi.Content = listNameWithSerialNum;
            ListBox_Playlist.Items.Add(lbi);
        }

        private void ListBox_Playlist_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBox_Playlist.SelectedItems.Count == 1)
            {
                RefreshPlaylistContent(((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString());
            }
        }

        private void ListBox_Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && ListBox_Playlist.SelectedItems.Count == 1)
            {
                // Remove playlist from database
                PlaylistCollection.RemovePlaylist(((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString());
                // Remove ListBoxItem
                ListBox_Playlist.Items.Remove(ListBox_Playlist.SelectedItem);
            }
        }
    }

    public class EmptyPlaylistException : Exception
    {
        public EmptyPlaylistException(string message) : base(message)
        { }
    }
}
