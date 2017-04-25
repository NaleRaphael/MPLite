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
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
    using PlayTrackEventArgs = Core.PlayTrackEventArgs;
    using SchedulerEventArgs = Event.SchedulerEventArgs;
    using PlaybackMode = Core.PlaybackMode;
    using PlaybackCommands = Event.PlaybackCommands;

    public partial class PagePlaylist : Page
    {
        #region Event
        public delegate void PlayTrackEventHandler(string selectedPlaylist = null, int selectedTrackIndex = -1,
            PlaybackMode mode = PlaybackMode.None);
        public static event PlayTrackEventHandler PlayTrackEvent;
        public delegate void NewSelectionEventHandler();    // User selected a track as a new entry of trackQueue
        public static event NewSelectionEventHandler NewSelectionEvent;

        //public delegate void StopPlayerRequestEventHandler(PlayTrackEventArgs e);
        //public static event StopPlayerRequestEventHandler StopPlayerRequestEvent;
        //public delegate void PausePlayerRequestEventHandler();
        //public static event PausePlayerRequestEventHandler PausePlayerRequestEvent;

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

        public ObservableCollection<Playlist> OCPlaylist { get; set; }
        public ObservableCollection<TrackInfo> OCTrack { get; set; }
        #endregion

        public PagePlaylist()
        {
            InitializeComponent();
            InitPlaylist();

            lb_PlaylistMenu.SelectedIndex = Properties.Settings.Default.LastSelectedPlaylistIndex;  // select default list
            currShowingPlaylist = Properties.Settings.Default.LastSelectedPlaylist;

            MainWindow.GetTrackEvent += GetTrack;
            MainWindow.TrackIsPlayedEvent += SetTrackStatus;
            MainWindow.TrackIsStoppedEvent += ResetTrackStatus;
            MainWindow.FailedToPlayTrackEvent += SetTrackStatus;
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

                if (Properties.Settings.Default.TaskPlaylist == currShowingPlaylist)
                    SetPlayingStateOfTrack(currTrackIdx, ">");
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
        public void ResetTrackStatus(PlayTrackEventArgs e)
        {
            string listName = Properties.Settings.Default.TaskPlaylist;

            // Check whether selected list is the one hosting the playing track.
            // (NOTE: we don't need to worry about those status wasn't cleared before switch to another playlist.
            // Because the content of `lv_playlist` will be reloaded from `MPlitePlaylist.json` when selected playlist is changed, 
            // and `trackInfo.PlayingSign` won't be store into it. 
            // So we just only have to set `PlayingSign` when the selected playlist is the one hosting playing track.)
            if (listName == currShowingPlaylist)
                SetPlayingStateOfTrack(e.PrevTrackIndex, MPLiteConstant.TrackStatusSign[(int)e.PrevTrackStatus]);
        }

        public void SetTrackStatus(PlayTrackEventArgs e)
        {
            string listName = Properties.Settings.Default.TaskPlaylist;
            if (listName == currShowingPlaylist)
                SetPlayingStateOfTrack(e.CurrTrackIndex, MPLiteConstant.TrackStatusSign[(int)e.CurrTrackStatus]);
        }

        public void SetTrackStatus()
        {
            string listName = Properties.Settings.Default.TaskPlaylist;
        }

        private void SetPlayingStateOfTrack(int trackIdx, string status)
        {
            if (trackIdx == -1) return;
            OCTrack[trackIdx].PlayingSign = status;
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

            NewSelectionEvent();    // Notify MusicPlayer to reset queue
            //StopPlayerRequestEvent(new PlayTrackEventArgs());
            StopPlayerRequestEvent();

            Properties.Settings.Default.TaskPlaybackMode = Properties.Settings.Default.PlaybackMode;
            Properties.Settings.Default.Save();
            PlayTrackEvent(listName, selIdx, (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode);
            //PlaySoundtrack(listName, selIdx, (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode);
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
        //private PlayTrackEventArgs GetTrack(MusicPlayer player, string selectedPlaylist = null,
        //    int selectedTrackIndex = -1, PlaybackMode mode = PlaybackMode.None, bool selectNext = true)
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
            /*
            string listName = ((Playlist)(((ListBoxItem)sender).Content)).ListName;
            SetPrevAndCurrShowingPlaylist(listName);
            RefreshPlaylistContent(listName, lb_PlaylistMenu.SelectedIndex);

            if (lv_Playlist.Visibility == Visibility.Hidden)
                lv_Playlist.Visibility = Visibility.Visible;
            */

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
                    string prevShowingPlaylist = ((Playlist)(lb_PlaylistMenu.Items[lb_PlaylistMenu.Items.Count - 1])).ListName;
                    RefreshPlaylistContent(prevShowingPlaylist, lb_PlaylistMenu.Items.Count - 1);
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
                TextBox txtBox = ((ListBoxItem)sender).Template.FindName("txtEditBox", (FrameworkElement)sender) as TextBox;
                if (txtBox == null)
                    Console.WriteLine("Failed to cast object to Textbox: ListBox - txtEditBox");
                TextBlock txtBlock = ((ListBoxItem)sender).Template.FindName("txtItemName", (FrameworkElement)sender) as TextBlock;
                if (txtBlock == null)
                    Console.WriteLine("Failed to cast object to Textbox: ListBox - txtItemName");

                oriPlaylistName = OCPlaylist[lb_PlaylistMenu.SelectedIndex].ListName;

                // Exchange the visibility of  control in selected TextBoxItem
                txtBlock.Visibility = Visibility.Hidden;
                txtBox.Visibility = Visibility.Visible;
                txtBox.Focus();     // Set focus on the TextBox of selected ListBoxItem
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
            TextBlock txtBlock = lbi.Template.FindName("txtItemName", lbi) as TextBlock;
            TextBox txtBox = sender as TextBox;

            // Exchange the visibility of  control in selected TextBoxItem
            txtBlock.Visibility = Visibility.Visible;
            txtBox.Visibility = Visibility.Hidden;

            Guid targetGuid = OCPlaylist[lb_PlaylistMenu.SelectedIndex].GUID;
            if (txtBox.Text != oriPlaylistName)
            {
                PlaylistCollection plc = PlaylistCollection.GetDatabase();
                Playlist pl = plc.TrackLists.Find(x => x.GUID == targetGuid);
                pl.ListName = txtBox.Text;
                plc.SaveToDatabase();
                lb_PlaylistMenu.UpdateLayout();
            }

            oriPlaylistName = "";   // Reset
        }
        #endregion

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
