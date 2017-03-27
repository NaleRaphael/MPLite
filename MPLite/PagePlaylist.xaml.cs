using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Threading;

namespace MPLite
{
    public partial class PagePlaylist : Page
    {
        //public delegate void PlayTrackEventHandler(TrackInfo trackInfo);
        public delegate void PlayTrackEventHandler();
        public static event PlayTrackEventHandler PlayTrackEvent;
        public delegate void NewSelectionEventHandler();    // User selected a track as a new entry of trackQueue
        public static event NewSelectionEventHandler NewSelectionEvent;

        private int idxOfPlayingTrack = -1;
        //private string listHostingPlayingTrack = Properties.Settings.Default.LastSelectedPlaylist;
        private string listHostingPlayingTrack;

        // Workaround for avoid playing wrong song when there are duplicates
        private int prevTrackIdx = -1;
        private int currTrackIdx = -1;

        public PagePlaylist()
        {
            InitializeComponent();
            InitPlaylist();
            lb_PlayistMenu.SelectedIndex = 0;  // select default list
            MainWindow.GetTrackEvent += MainWindow_GetTrackEvent;
            MainWindow.TrackIsPlayedEvent += SetTrackStatus;
            MainWindow.TrackIsStoppedEvent += ResetTrackStatus;
        }

        public void ResetTrackStatus(TrackInfo track)
        {
            string listName = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();

            // Check whether selected list is the one hosting the playing track.
            // If not, just reset idxOfPlayingTrack and listHostingPlayingTrack
            if (listName == listHostingPlayingTrack)
            {
                SetPlayingStateOfTrack(prevTrackIdx, "");
            }
            //listHostingPlayingTrack = "";
            //idxOfPlayingTrack = -1;
        }

        public void SetTrackStatus(TrackInfo track)
        {
            //listHostingPlayingTrack = Properties.Settings.Default.LastSelectedPlaylist;

            string listName = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
            if (listName == listHostingPlayingTrack)
            {
                SetPlayingStateOfTrack(currTrackIdx, ">");
            }
            //SetPlayingStateOfTrack(currTrackIdx, ">");
        }

        private void SetPlayingStateOfTrack(int trackIdx, string status)
        {
            if (trackIdx == -1)
                return;
            TrackInfo selectedTrack = lv_Playlist.Items.OfType<TrackInfo>().ToList()[trackIdx];
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
                lb_PlayistMenu.Items.Add(lvi);
            }
        }

        private void RefreshPlaylistContent(string selectedPlaylist)
        {
            // Check whether there has been some playlist in lb_PlayistMenu.
            // If not, lv_Playlist show be hidden.
            if (lb_PlayistMenu.Items.Count == 0)
                lv_Playlist.Visibility = Visibility.Hidden;

            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;
            Playlist pl = plc.TrackLists.Find(x => x.ListName == selectedPlaylist);

            lv_Playlist.Items.Clear();
            if (pl != null)
            {
                foreach (TrackInfo track in pl.Soundtracks)
                {
                    lv_Playlist.Items.Add(track);
                }
            }

            // Update info
            Properties.Settings.Default.LastSelectedPlaylist = selectedPlaylist;
            Properties.Settings.Default.Save();

            // Show playing track
            if ((ListBoxItem)lb_PlayistMenu.SelectedItem != null)
            {
                string listName = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
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
            string selectedPlaylist = ((ListBoxItem)lb_PlayistMenu.SelectedValue).Content.ToString();
            int cnt = 0;    // Counter for preventing unnesaccery update of database

            // Update LV_Playlist
            foreach (string filePath in files)
            {
                if (!MPLiteConstant.validFileType.Contains(System.IO.Path.GetExtension(filePath)))
                    continue;

                // TODO: rewrite this
                //       Store data into a dataset (json file)
                //       Then present those information into listview
                lv_Playlist.Items.Add(TrackInfo.ParseSource(filePath));

                cnt++;
            }

            // Update database
            if (cnt != 0)
                PlaylistCollection.Update(files, selectedPlaylist);
        }

        private void LV_Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int selIdx = lv_Playlist.SelectedIndex;
            if (selIdx < 0) return;

            listHostingPlayingTrack = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
            prevTrackIdx = currTrackIdx;    // workaround
            currTrackIdx = selIdx;          // workaround

            NewSelectionEvent();    // Notify MusicPlay to reset queue
            PlaySoundtrack(currTrackIdx);
        }

        private void LV_Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lv_Playlist.SelectedItems.Count != 0)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlayistMenu.SelectedValue).Content.ToString();
                List<int> selectedIdx = new List<int>();

                foreach (TrackInfo track in lv_Playlist.SelectedItems)
                {
                    selectedIdx.Add(lv_Playlist.Items.IndexOf(track));
                }

                selectedIdx.Sort((x, y) => { return -x.CompareTo(y); });
                foreach (int idx in selectedIdx)
                {
                    lv_Playlist.Items.RemoveAt(idx);
                }

                // UPDATE database
                PlaylistCollection.DeleteTracksByIndices(selectedIdx.ToArray<int>(), selectedPlaylist);
            }
        }

        /*private TrackInfo GetSoundtrack(int selIdx)
        {
            Playlist pl = PlaylistCollection.GetPlaylist(((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString());
            if (pl == null)
                throw new InvalidPlaylistException("Invalid playlist.");
            currTrackIdx = selIdx;      // workaround
            return pl.Soundtracks[currTrackIdx];
        }*/

        private void PlaySoundtrack(int selIdx)
        {
            // Fire event to start playing track
            try
            {
                //PlayTrackEvent(GetSoundtrack(selIdx));
                PlayTrackEvent();

                // Showing current playing track in status bar


                // Remember which track is playing
                //prevTrackIdx = currTrackIdx;
                //currTrackIdx = selIdx;
                //listHostingPlayingTrack = ((ListBoxItem)(ListBox_Playlist.SelectedItem)).Content.ToString();
            }
            catch
            {
                throw;
            }
        }

        private TrackInfo MainWindow_GetTrackEvent(MusicPlayer player)
        {
            if (lb_PlayistMenu.Items.Count == 0 || lv_Playlist.Items.Count == 0)
            {
                throw new EmptyPlaylistException("No tracks avalible");
            }

            TrackInfo track;

            //idxOfPlayingTrack = (LV_Playlist.SelectedIndex > 0) ? LV_Playlist.SelectedIndex : 0;
            //track = player.GetNextTrack(playlist, LV_Playlist.SelectedIndex);

            try
            {
                //track = GetSoundtrack(idxOfPlayingTrack);
                Playlist playlist = PlaylistCollection.GetPlaylist(listHostingPlayingTrack);
                //Playlist playlist = PlaylistCollection.GetPlaylist(((ListBoxItem)ListBox_Playlist.SelectedItem).Content.ToString());

                //cur = player.GetNextTrack(playlist, LV_Playlist.SelectedIndex);

                prevTrackIdx = currTrackIdx;    // workaround
                track = player.GetNextTrack(playlist, currTrackIdx, out currTrackIdx);
                idxOfPlayingTrack = currTrackIdx;
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
            lb_PlayistMenu.Items.Add(lbi);

            // Focus on it
            lb_PlayistMenu.SelectedItem = lbi;

            // Refresh LV_Playlist
            RefreshPlaylistContent(listNameWithSerialNum);

            lv_Playlist.Visibility = Visibility.Visible;
        }

        private void ListBox_Playlist_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lb_PlayistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
                RefreshPlaylistContent(selectedPlaylist);
                Properties.Settings.Default.LastSelectedPlaylist = selectedPlaylist;
                Properties.Settings.Default.Save();

                if (lv_Playlist.Visibility == Visibility.Hidden)
                    lv_Playlist.Visibility = Visibility.Visible;
            }
        }

        private void ListBox_Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lb_PlayistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
                // Remove playlist from database
                PlaylistCollection.RemovePlaylist(selectedPlaylist);
                // Refresh LV_Playlist
                RefreshPlaylistContent(selectedPlaylist);
                // Remove ListBoxItem
                lb_PlayistMenu.Items.Remove(lb_PlayistMenu.SelectedItem);

                // Switch content of lv_playlist to previous one, or hide lv_playlist if there is no remaining items in lb_PlaylistMenu
                if (lb_PlayistMenu.Items.Count != 0)
                {
                    string prevPlaylist = ((ListBoxItem)(lb_PlayistMenu.Items[lb_PlayistMenu.Items.Count - 1])).Content.ToString();
                    RefreshPlaylistContent(prevPlaylist);
                }
                else
                {
                    lv_Playlist.Visibility = Visibility.Hidden;
                }
            }
        }
    }

    public class EmptyPlaylistException : Exception
    {
        public EmptyPlaylistException(string message) : base(message)
        { }
    }
}
