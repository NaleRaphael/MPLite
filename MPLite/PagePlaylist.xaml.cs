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
        #region Event
        public delegate void PlayTrackEventHandler(string selectedPlaylist = null, int selectedTrackIndex = -1, 
            MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None);
        public static event PlayTrackEventHandler PlayTrackEvent;
        public delegate void NewSelectionEventHandler();    // User selected a track as a new entry of trackQueue
        public static event NewSelectionEventHandler NewSelectionEvent;
        public delegate void StopPlayerRequestEventHandler(PlayTrackEventArgs e);
        public static event StopPlayerRequestEventHandler StopPlayerRequestEvent;
        #endregion

        #region Field
        private string prevShowingPlaylist;
        private string currShowingPlaylist;

        // Workaround of avoid playing wrong song when there are duplicates
        private int prevTrackIdx = -1;
        private int currTrackIdx = -1;
        #endregion

        public PagePlaylist()
        {
            InitializeComponent();
            InitPlaylist();

            lb_PlaylistMenu.SelectedIndex = Properties.Settings.Default.LastSelectedPlaylistIndex;  // select default list
            currShowingPlaylist = Properties.Settings.Default.LastSelectedPlaylist;

            MainWindow.GetTrackEvent += MainWindow_GetTrackEvent;
            MainWindow.TrackIsPlayedEvent += SetTrackStatus;
            MainWindow.TrackIsStoppedEvent += ResetTrackStatus;
            MainWindow.FailedToPlayTrackEvent += SetTrackStatus;
        }

        #region Initialization
        private void InitPlaylist()
        {
            RefreshPlaylist();
            RefreshPlaylistContent(Properties.Settings.Default.LastSelectedPlaylist, Properties.Settings.Default.LastSelectedPlaylistIndex);
        }

        private void RefreshPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            foreach (Playlist pl in plc.TrackLists)
            {
                ListBoxItem lvi = new ListBoxItem();
                lvi.Content = pl.ListName;
                lb_PlaylistMenu.Items.Add(lvi);
            }
        }

        private void RefreshPlaylistContent(string selectedPlaylist, int selectedPlaylistIndex)
        {
            // Check whether there has been some playlist in lb_PlayistMenu. If not, lv_Playlist show be hidden.
            if (lb_PlaylistMenu.Items.Count == 0)
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
            // ... workaround: to select playlist in the stage of initialization
            Properties.Settings.Default.LastSelectedPlaylistIndex = selectedPlaylistIndex;
            Properties.Settings.Default.Save();

            // Show playing track
            if ((ListBoxItem)lb_PlaylistMenu.SelectedItem != null)
            {
                string listName = ((ListBoxItem)lb_PlaylistMenu.SelectedItem).Content.ToString();

                if (Properties.Settings.Default.TaskPlaylist == currShowingPlaylist)
                {
                    SetPlayingStateOfTrack(currTrackIdx, ">");
                }
            }
        }
        #endregion

        #region Track status control
        public void ResetTrackStatus(PlayTrackEventArgs e)
        {
            string listName = Properties.Settings.Default.TaskPlaylist;

            // Check whether selected list is the one hosting the playing track.
            // (NOTE: we don't need to worry about those status wasn't cleared before switch to another playlist.
            // Because the content of `lv_playlist` will be reloaded from `MPlitePlaylist.json` when selected playlist is changed, 
            // and `trackInfo.PlayingSign` won't be store into it. 
            // So we just only have to set `PlayingSign` when the selected playlist is the one hosting playing track.)
            if (listName == currShowingPlaylist)
            {
                SetPlayingStateOfTrack(e.PrevTrackIndex, MPLiteConstant.TrackStatusSign[(int)e.PrevTrackStatus]);
            }
        }

        public void SetTrackStatus(PlayTrackEventArgs e)
        {
            string listName = Properties.Settings.Default.TaskPlaylist;
            if (listName == currShowingPlaylist)
            {
                SetPlayingStateOfTrack(e.CurrTrackIndex, MPLiteConstant.TrackStatusSign[(int)e.CurrTrackStatus]);
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
        #endregion

        #region lb_Playlist controls
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
            string selectedPlaylist = ((ListBoxItem)lb_PlaylistMenu.SelectedValue).Content.ToString();
            int cnt = 0;    // Counter for preventing unnesaccery update of database

            // Update lv_Playlist
            foreach (string filePath in files)
            {
                if (!MPLiteConstant.validFileType.Contains(System.IO.Path.GetExtension(filePath)))
                    continue;

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

            string playlistName = ((ListBoxItem)lb_PlaylistMenu.SelectedItem).Content.ToString();
            SetPrevAndCurrShowingPlaylist(((ListBoxItem)lb_PlaylistMenu.SelectedItem).Content.ToString());

            NewSelectionEvent();    // Notify MusicPlayer to reset queue
            StopPlayerRequestEvent(new PlayTrackEventArgs());
            PlaySoundtrack(playlistName, selIdx);
        }

        private void lv_Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lv_Playlist.SelectedItems.Count != 0)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlaylistMenu.SelectedValue).Content.ToString();
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
        #endregion

        private void SetPrevAndCurrShowingPlaylist(string newPlaylist)
        {
            prevShowingPlaylist = currShowingPlaylist;
            currShowingPlaylist = newPlaylist;
        }

        private void PlaySoundtrack(string selectedPlaylist = null, int selectedTrackIndex = -1, 
            MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None)
        {
            try
            {
                // Fire event to notify MainWindow that a track is selected and waiting to be played
                PlayTrackEvent(selectedPlaylist, selectedTrackIndex, mode);
            }
            catch
            {
                throw;
            }
        }

        private PlayTrackEventArgs MainWindow_GetTrackEvent(MusicPlayer player, string selectedPlaylist = null, 
            int selectedTrackIndex = -1, MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None)
        {
            if (lb_PlaylistMenu.Items.Count == 0 || lv_Playlist.Items.Count == 0)
            {
                throw new EmptyPlaylistException("No tracks avalible");
            }

            PlayTrackEventArgs e;
            try
            {
                // If no playlist is selected (user click btn_StartPlayback to play music)
                selectedPlaylist = (selectedPlaylist == null) ? currShowingPlaylist : selectedPlaylist;

                // Track info will be stored into e
                e = GetTrack(player, selectedPlaylist, selectedTrackIndex, mode);
            }
            catch
            {
                throw;
            }
            return e;
        }

        private PlayTrackEventArgs GetTrack(MusicPlayer player, string selectedPlaylist = null,
            int selectedTrackIndex = -1, MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None)
        {
            PlayTrackEventArgs e;
            try
            {
                prevTrackIdx = currTrackIdx;    // workaround
                e = player.GetNextTrack(selectedPlaylist, selectedTrackIndex, mode, out currTrackIdx);
            }
            catch
            {
                throw;
            }
            return e;
        }

        private void btn_AddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = new ListBoxItem();
            string newListName = "New Playlist";  // TODO: add serial number

            // Add to database
            string listNameWithSerialNum = PlaylistCollection.AddPlaylist(newListName);

            // Add to ListBox
            lbi.Content = listNameWithSerialNum;
            lb_PlaylistMenu.Items.Add(lbi);

            // Focus on it
            lb_PlaylistMenu.SelectedItem = lbi;

            // Refresh lv_Playlist
            RefreshPlaylistContent(listNameWithSerialNum, lb_PlaylistMenu.Items.IndexOf(lbi));

            lv_Playlist.Visibility = Visibility.Visible;
        }

        private void lb_PlaylistMenu_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lb_PlaylistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlaylistMenu.SelectedItem).Content.ToString();
                SetPrevAndCurrShowingPlaylist(selectedPlaylist);
                RefreshPlaylistContent(selectedPlaylist, lb_PlaylistMenu.SelectedIndex);
                Properties.Settings.Default.LastSelectedPlaylist = selectedPlaylist;
                Properties.Settings.Default.Save();

                if (lv_Playlist.Visibility == Visibility.Hidden)
                    lv_Playlist.Visibility = Visibility.Visible;
            }
        }

        private void lb_PlaylistMenu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lb_PlaylistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((ListBoxItem)lb_PlaylistMenu.SelectedItem).Content.ToString();
                // Remove playlist from database
                PlaylistCollection.RemovePlaylist(selectedPlaylist);
                // Refresh lv_Playlist
                RefreshPlaylistContent(selectedPlaylist, lb_PlaylistMenu.SelectedIndex);
                // Remove ListBoxItem
                lb_PlaylistMenu.Items.Remove(lb_PlaylistMenu.SelectedItem);

                // Switch content of lv_playlist to previous one, or hide lv_playlist if there is no remaining items in lb_PlaylistMenu
                if (lb_PlaylistMenu.Items.Count != 0)
                {
                    string prevShowingPlaylist = ((ListBoxItem)(lb_PlaylistMenu.Items[lb_PlaylistMenu.Items.Count - 1])).Content.ToString();
                    RefreshPlaylistContent(prevShowingPlaylist, lb_PlaylistMenu.Items.Count - 1);
                }
                else
                {
                    lv_Playlist.Visibility = Visibility.Hidden;
                }
            }
        }

        #region Handle events fired from scheduler
        public void RunPlaylist(string selectedPlaylist = null, int selectedTrackIndex = -1,
            MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None)
        {
            NewSelectionEvent();    // Notify MusicPlayer to reset queue
            StopPlayerRequestEvent(new PlayTrackEventArgs());
            PlaySoundtrack(selectedPlaylist, selectedTrackIndex, mode);
        }
        #endregion
    }

    public class EmptyPlaylistException : Exception
    {
        public EmptyPlaylistException(string message) : base(message)
        {
        }
    }
}
