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
        public delegate void PlayTrackEventHandler(string playlistName, int selectedIdx);
        public static event PlayTrackEventHandler PlayTrackEvent;
        public delegate void NewSelectionEventHandler();    // User selected a track as a new entry of trackQueue
        public static event NewSelectionEventHandler NewSelectionEvent;

        private int idxOfPlayingTrack = -1;
        private string prevPlaylist;
        private string currPlaylist;

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
            //PageCalendar.SchedulerIsTriggeredEvent;
        }

        public void ResetTrackStatus(TrackInfo track)
        {
            string listName = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();

            // Check whether selected list is the one hosting the playing track.
            // (NOTE: we don't need to worry about those status wasn't cleared before switch to another playlist.
            // Because the content of `lv_playlist` will be reloaded from `MPlitePlaylist.json` when selected playlist is changed, 
            // and `trackInfo.PlayingSign` won't be store into it. 
            // So we just only have to set `PlayingSign` when the selected playlist is the one hosting playing track.)
            if (listName == prevPlaylist)
            {
                SetPlayingStateOfTrack(prevTrackIdx, "");
            }
        }

        public void SetTrackStatus(TrackInfo track)
        {
            //listHostingPlayingTrack = Properties.Settings.Default.LastSelectedPlaylist;

            string listName = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
            if (listName == currPlaylist)
            {
                SetPlayingStateOfTrack(currTrackIdx, ">");
            }
        }

        private void SetPlayingStateOfTrack(int trackIdx, string status)
        {
            if (trackIdx == -1)
                return;
            TrackInfo selectedTrack = lv_Playlist.Items.OfType<TrackInfo>().ToList()[trackIdx];
            selectedTrack.PlayingSign = status;
            lv_Playlist.UpdateLayout();
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
                if (listName == currPlaylist)
                {
                    SetPlayingStateOfTrack(currTrackIdx, ">");
                }
            }
        }

        private void lv_Playlist_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void lv_Playlist_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string selectedPlaylist = ((ListBoxItem)lb_PlayistMenu.SelectedValue).Content.ToString();
            int cnt = 0;    // Counter for preventing unnesaccery update of database

            // Update lv_Playlist
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

        private void lv_Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int selIdx = lv_Playlist.SelectedIndex;
            if (selIdx < 0) return;

            string playlistName = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
            //SetPrevAndCurrPlaylist(((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString());

            NewSelectionEvent();    // Notify MusicPlayer to reset queue
            PlaySoundtrack(playlistName, selIdx);
        }

        private void SetPrevAndCurrPlaylist(string newPlaylist)
        {
            prevPlaylist = currPlaylist;
            currPlaylist = newPlaylist;
        }

        private void lv_Playlist_KeyDown(object sender, KeyEventArgs e)
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

        private void PlaySoundtrack(string playlistName, int selectedIdx)
        {
            try
            {
                // Fire event to notify MainWindow that a track is selected and waiting to be played
                PlayTrackEvent(playlistName, selectedIdx);   
            }
            catch
            {
                throw;
            }
        }

        private TrackInfo MainWindow_GetTrackEvent(MusicPlayer player, string playlistName, int selectedIdx)
        {
            if (lb_PlayistMenu.Items.Count == 0 || lv_Playlist.Items.Count == 0)
            {
                throw new EmptyPlaylistException("No tracks avalible");
            }

            TrackInfo track;
            try
            {
                playlistName = (playlistName == null) ? currPlaylist : playlistName;
                track = GetTrack(player, playlistName, lv_Playlist.SelectedIndex);
                SetPrevAndCurrPlaylist(playlistName);
            }
            catch
            {
                throw;
            }
            return track;
        }

        private TrackInfo GetTrack(MusicPlayer player, string playlistName, int selectedIdx)
        {
            TrackInfo track;
            try
            {
                prevTrackIdx = currTrackIdx;    // workaround
                track = player.GetNextTrack(playlistName, selectedIdx, out currTrackIdx);
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

            // Refresh lv_Playlist
            RefreshPlaylistContent(listNameWithSerialNum);

            lv_Playlist.Visibility = Visibility.Visible;
        }

        private void lb_PlaylistMenu_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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

        private void lb_PlaylistMenu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lb_PlayistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlayistMenu.SelectedItem).Content.ToString();
                // Remove playlist from database
                PlaylistCollection.RemovePlaylist(selectedPlaylist);
                // Refresh lv_Playlist
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

        #region Handle events fired from scheduler
        public void RunPlaylist(string playlistName, int selectedIdx)
        {
            SetPrevAndCurrPlaylist(playlistName);

            NewSelectionEvent();    // Notify MusicPlayer to reset queue

            PlaySoundtrack(playlistName, selectedIdx);

            /*TrackInfo track;
            try
            {
                prevTrackIdx = currTrackIdx;    // workaround
                track = player.GetNextTrack(currPlaylist, lv_Playlist.SelectedIndex, out currTrackIdx);
                idxOfPlayingTrack = currTrackIdx;
            }
            catch
            {
                throw;
            }
            return track;*/
        }
        #endregion
    }

    public class EmptyPlaylistException : Exception
    {
        public EmptyPlaylistException(string message) : base(message)
        { }
    }
}
