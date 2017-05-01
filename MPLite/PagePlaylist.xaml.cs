using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MPLite
{
    using TrackInfo = Core.TrackInfo;
    using MPLiteConstant = Core.MPLiteConstant;
    using TrackStatus = Core.TrackStatus;
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
    using PlayTrackEventArgs = Core.PlayTrackEventArgs;
    using SchedulerEventArgs = Event.SchedulerEventArgs;
    using PlaybackMode = Core.PlaybackMode;
    using PlaybackCommands = Event.PlaybackCommands;

    public partial class PagePlaylist : Page
    {
        public ObservableCollection<Playlist> OCPlaylist { get; set; }
        public ObservableCollection<TrackInfo> OCTrack { get; set; }

        #region Event
        public delegate void PlayTrackEventHandler(string selectedPlaylist = null, int selectedTrackIndex = -1,
            PlaybackMode mode = PlaybackMode.None);
        public static event PlayTrackEventHandler PlayTrackEvent;
        public delegate void NewSelectionEventHandler();    // User selected a track as a new entry of trackQueue
        public static event NewSelectionEventHandler NewSelectionEvent;
        public delegate TrackStatusEventArgs ListContentIsRefreshedEventHandler();
        public static event ListContentIsRefreshedEventHandler ListContentIsRefreshedEvent;

        public delegate void StopPlayerRequestEventHandler();
        public static event StopPlayerRequestEventHandler StopPlayerRequestEvent;
        public delegate void PausePlayerRequestEventHandler();
        public static event PausePlayerRequestEventHandler PausePlayerRequestEvent;

        #endregion

        #region Field
        private string oriPlaylistName = "";

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

            MainWindow.GetTrackEvent += GetTrack;
        }

        #region Initialization
        private void InitPlaylist()
        {
            if (OCPlaylist == null) OCPlaylist = new ObservableCollection<Playlist>();
            if (OCTrack == null) OCTrack = new ObservableCollection<TrackInfo>();
            lb_PlaylistMenu.ItemsSource = OCPlaylist;
            lv_Playlist.ItemsSource = OCTrack;

            RefreshPlaylist();
            RefreshPlaylistContent(Properties.Settings.Default.LastSelectedPlaylist, Properties.Settings.Default.LastSelectedPlaylistIndex);
        }

        private void RefreshPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            FillOC(OCPlaylist, plc.TrackLists);
        }

        private void RefreshPlaylistContent(string selectedPlaylist, int selectedPlaylistIndex)
        {
            // Check whether there has been some playlist in lb_PlayistMenu. If not, lv_Playlist show be hidden.
            if (lb_PlaylistMenu.Items.Count == 0)
                lv_Playlist.Visibility = Visibility.Hidden;

            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;
            Playlist pl = plc.TrackLists.Find(x => x.ListName == selectedPlaylist);
            if (pl == null) return;

            FillOC(OCTrack, pl.Soundtracks);

            // Update info
            Properties.Settings.Default.LastSelectedPlaylist = selectedPlaylist;
            // ... workaround: to select playlist in the stage of initialization
            Properties.Settings.Default.LastSelectedPlaylistIndex = selectedPlaylistIndex;
            Properties.Settings.Default.Save();

            // Show playing track
            if (lb_PlaylistMenu.SelectedItem != null)
            {
                string listName = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;

                if (Properties.Settings.Default.TaskPlaylist == listName)
                {
                    SetTrackStatus(ListContentIsRefreshedEvent());
                }
            }
        }

        private void FillOC<T>(ObservableCollection<T> oc, List<T> source)
        {
            if (source == null) return;

            oc.Clear();
            foreach (T obj in source)
                oc.Add(obj);
        }
        #endregion

        #region Track status control
        public void SetTrackStatus(TrackStatusEventArgs e)
        {
            if (e == null || ((Playlist)lb_PlaylistMenu.SelectedItem).ListName != e.OwnerList)
                return;
            if (e.Index == -1 || e.Index > OCTrack.Count)
                return;
            OCTrack[e.Index].StatusSign = MPLiteConstant.TrackStatusSign[(int)e.Track.TrackStatus];
        }
        #endregion

        #region lv_Playlist controls
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
            string listName = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
            List<string> flist = new List<string>();
            List<TrackInfo> tlist = new List<TrackInfo>();

            foreach(string filePath in files)
            {
                // Validate file type
                if (!MPLiteConstant.validFileType.Contains(System.IO.Path.GetExtension(filePath)))
                    continue;

                try
                {
                    TrackInfo track = TrackInfo.ParseSource(filePath);
                    tlist.Add(track);
                    OCTrack.Add(track);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            // Update database
            if (tlist.Count != 0)
                PlaylistCollection.AddPlaylist(tlist, listName);
        }

        private void lv_Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int selIdx = lv_Playlist.SelectedIndex;
            if (selIdx < 0) return;

            string listName = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
            SetPrevAndCurrShowingPlaylist(listName);

            StopPlayerRequestEvent();
            NewSelectionEvent();    // Notify MusicPlayer to reset queue

            Properties.Settings.Default.TaskPlaybackMode = Properties.Settings.Default.PlaybackMode;
            Properties.Settings.Default.TaskPlaylist = listName;
            Properties.Settings.Default.Save();
            PlayTrackEvent(listName, selIdx, (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode);
        }

        private void lv_Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lv_Playlist.SelectedItems.Count != 0)
            {
                string listName = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
                List<int> selIdices = new List<int>();

                foreach (TrackInfo track in lv_Playlist.SelectedItems)
                    selIdices.Add(lv_Playlist.Items.IndexOf(track));

                selIdices.Sort((x, y) => { return -x.CompareTo(y); });
                PlaylistCollection.DeleteTracksByIndices(selIdices.ToArray<int>(), listName);

                foreach (int i in selIdices)
                    OCTrack.RemoveAt(i);
            }
        }
        #endregion

        private void SetPrevAndCurrShowingPlaylist(string newPlaylist)
        {
            prevShowingPlaylist = currShowingPlaylist;
            currShowingPlaylist = newPlaylist;

            Properties.Settings.Default.LastSelectedPlaylist = newPlaylist;
            Properties.Settings.Default.Save();
        }

        private void PlaySoundtrack(string selectedPlaylist = null, int selectedTrackIndex = -1,
            PlaybackMode mode = PlaybackMode.None)
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

        // TODO: add a parameter: get prev/next
        private TrackInfo GetTrack(MusicPlayer player, string selectedPlaylist = null,
            int selectedTrackIndex = -1, PlaybackMode mode = PlaybackMode.None, bool selectNext = true)
        {
            if (lb_PlaylistMenu.Items.Count == 0 || lv_Playlist.Items.Count == 0)
            {
                throw new EmptyPlaylistException("No tracks avalible");
            }

            try
            {
                // If no playlist is selected (user click btn_StartPlayback to play music)
                selectedPlaylist = (selectedPlaylist == null) ? currShowingPlaylist : selectedPlaylist;

                if (mode == PlaybackMode.None)
                    throw new Exception("Invalid playback mode");

                if (selectNext)
                {
                    prevTrackIdx = currTrackIdx;    // workaround
                    return player.GetTrack(selectedPlaylist, selectedTrackIndex, mode);
                }
                else
                {
                    currTrackIdx = prevTrackIdx;
                    return player.GetPrevTrack(selectedPlaylist, selectedTrackIndex, mode);
                }
            }
            catch
            {
                throw;
            }
        }

        private void btnAddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = new ListBoxItem();
            string newListName = "New Playlist";  // TODO: add serial number

            // Add to database
            string listNameWithSerialNum = PlaylistCollection.AddPlaylist(newListName);

            // Add to ListBox
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            Playlist pl = plc.TrackLists.Find(x => x.ListName == listNameWithSerialNum);
            OCPlaylist.Add(pl);

            // Focus on it
            lb_PlaylistMenu.SelectedItem = lbi;

            // Refresh lv_Playlist
            RefreshPlaylistContent(listNameWithSerialNum, lb_PlaylistMenu.Items.IndexOf(lbi));

            lv_Playlist.Visibility = Visibility.Visible;
        }

        #region Playlist menu control
        private void MenuListBox_MouseUp(object sender, MouseEventArgs e)
        {
            lb_PlaylistMenu_SelectionChanged(null, null);
        }

        private void lb_PlaylistMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string listName = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
            SetPrevAndCurrShowingPlaylist(listName);
            RefreshPlaylistContent(listName, lb_PlaylistMenu.SelectedIndex);

            if (lv_Playlist.Visibility == Visibility.Hidden)
                lv_Playlist.Visibility = Visibility.Visible;
        }

        private void lb_PlaylistMenu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lb_PlaylistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
                // Remove playlist from database
                PlaylistCollection.RemovePlaylist(selectedPlaylist);
                // Refresh lv_Playlist
                RefreshPlaylistContent(selectedPlaylist, lb_PlaylistMenu.SelectedIndex);
                // Remove ListBoxItem
                OCPlaylist.Remove((Playlist)lb_PlaylistMenu.SelectedItem);

                // Switch content of lv_playlist to previous one, or hide lv_playlist if there is no remaining items in lb_PlaylistMenu
                if (lb_PlaylistMenu.Items.Count != 0)
                {
                    selectedPlaylist = ((Playlist)(lb_PlaylistMenu.Items[lb_PlaylistMenu.Items.Count - 1])).ListName;
                    RefreshPlaylistContent(selectedPlaylist, lb_PlaylistMenu.Items.Count - 1);
                }
                else
                {
                    lv_Playlist.Visibility = Visibility.Hidden;
                }
            }
        }

        private void MenuListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)    // Rename listName in the ListBoxItem
            {
                SwitchEditingMode((ListBoxItem)sender, true);
                oriPlaylistName = OCPlaylist[lb_PlaylistMenu.SelectedIndex].ListName;
            }
            else if (e.Key == Key.Escape)   // Disable editable mode of selected TextBoxItem
            {
                TextBox txtBox = ((ListBoxItem)sender).Template.FindName("txtEditBox", (FrameworkElement)sender) as TextBox;
                if (txtBox.IsFocused)
                    lb_PlaylistMenu.Focus();    // Transfer focus
                oriPlaylistName = "";   // Reset
            }
            else if (e.Key == Key.Enter)    // User confirms the new name
            {
                TextBox txtBox = ((ListBoxItem)sender).Template.FindName("txtEditBox", (FrameworkElement)sender) as TextBox;
                txtEditBox_LostFocus(txtBox, new RoutedEventArgs());    // Trigger LostFocus event to save change
            }
        }

        private void txtEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = ((TextBox)sender).TemplatedParent as ListBoxItem;
            string result = SwitchEditingMode(lbi, false);

            Guid targetGuid = OCPlaylist[lb_PlaylistMenu.SelectedIndex].GUID;
            if (result != oriPlaylistName)
            {
                PlaylistCollection plc = PlaylistCollection.GetDatabase();
                Playlist pl = plc.TrackLists.Find(x => x.GUID == targetGuid);
                pl.ListName = result;
                plc.SaveToDatabase();
                lb_PlaylistMenu.UpdateLayout();
            }

            oriPlaylistName = "";   // Reset
        }

        private string SwitchEditingMode(ListBoxItem parent, bool isEditingMode)
        {
            TextBox txtBox = parent.Template.FindName("txtEditBox", parent) as TextBox;
            TextBlock txtBlock = parent.Template.FindName("txtItemName", parent) as TextBlock;

            txtBlock.Visibility = isEditingMode ? Visibility.Hidden : Visibility.Visible;
            txtBox.Visibility = isEditingMode ? Visibility.Visible : Visibility.Hidden;
            if (isEditingMode)
            {
                txtBox.Focus();
                txtBox.CaretIndex = txtBox.Text.Length;
            }

            return isEditingMode ? null : txtBox.Text;
        }
        #endregion

        private void miMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;
            SwitchEditingMode(lbi, true);
        }

        #region Handle events fired from scheduler
        // TODO: rename this
        public void RunPlaylist(SchedulerEventArgs e)
        {
            NewSelectionEvent();    // Notify MusicPlayer to reset queue

            switch (e.Command)
            {
                case PlaybackCommands.Pause:
                    PausePlayerRequestEvent();
                    break;
                case PlaybackCommands.Stop:
                    StopPlayerRequestEvent();
                    break;
                case PlaybackCommands.Play:
                    StopPlayerRequestEvent();
                    PlaySoundtrack(e.Playlist, e.TrackIndex, e.Mode);
                    break;
                default:
                    // Add a handling process for this exception
                    //throw new Exception("Invalid command");
                    break;
            }
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
