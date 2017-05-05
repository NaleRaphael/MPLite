using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;

namespace MPLite
{
    using TrackInfo = Core.TrackInfo;
    using MPLiteConstant = Core.MPLiteConstant;
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
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
            lb_PlaylistMenu.DataContext = new TrackListsViewModel();
            OCPlaylist = (lb_PlaylistMenu.DataContext as TrackListsViewModel).TrackLists;
            lb_PlaylistMenu.ItemsSource = OCPlaylist;

            lv_Playlist.DataContext = new TracksViewModel(PlaylistCollection.GetPlaylist(Properties.Settings.Default.LastSelectedPlaylist));
            OCTrack = (lv_Playlist.DataContext as TracksViewModel).Soundtracks;
            lv_Playlist.ItemsSource = OCTrack;

            RefreshPlaylist();
            RefreshPlaylistContent(Properties.Settings.Default.LastSelectedPlaylist, Properties.Settings.Default.LastSelectedPlaylistIndex);
        }

        private void RefreshPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            (lb_PlaylistMenu.DataContext as TrackListsViewModel).UpdateTrackLists(plc.TrackLists);
            //(lb_PlaylistMenu.DataContext as TrackListsViewModel).TrackLists.FillContent(plc.TrackLists);
            //FillOC((lb_PlaylistMenu.DataContext as TrackListsViewModel).TrackLists, plc.TrackLists);
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

            (lv_Playlist.DataContext as TracksViewModel).UpdateSoundtracks(pl.Soundtracks);
            //(lv_Playlist.DataContext as TracksViewModel).Soundtracks.FillContent(pl.Soundtracks);
            //FillOC((lv_Playlist.DataContext as TracksViewModel).Soundtracks, pl.Soundtracks);

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
        #endregion

        #region Track status control
        public void SetTrackStatus(TrackStatusEventArgs e)
        {
            if (e == null || ((Playlist)lb_PlaylistMenu.SelectedItem).ListName != e.OwnerList)
                return;
            if (e.Index == -1 || e.Index > OCTrack.Count)
                return;
            OCTrack[e.Index].TrackStatus = e.Track.TrackStatus;
            OCTrack[e.Index].StatusSign = MPLiteConstant.TrackStatusSign[(int)e.Track.TrackStatus];
        }
        #endregion

        #region lv_Playlist controls
        private void lv_Playlist_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("DragEnter: " + e.Data.ToString());

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(lv_Playlist, false);
                GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(lv_Playlist, false);
                e.Effects = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
            }
        }

        private void lv_Playlist_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null)
                return;

            string listName = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
            //List<string> flist = new List<string>();
            List<TrackInfo> tlist = new List<TrackInfo>();

            foreach (string filePath in files)
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

                // Stop player if selected track is playing
                //if (((Playlist)lb_PlaylistMenu.SelectedItem).Soundtracks[])

                foreach (TrackInfo track in lv_Playlist.SelectedItems)
                {
                    selIdices.Add(lv_Playlist.Items.IndexOf(track));
                    if (track.TrackStatus == Core.TrackStatus.Playing)
                        StopPlayerRequestEvent();
                }

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
                    return player.GetTrack(selectedPlaylist, selectedTrackIndex, mode);
                }
                else
                {
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
            Playlist pl = lb_PlaylistMenu.SelectedItem as Playlist;
            TracksViewModel tvm = lv_Playlist.DataContext as TracksViewModel;
            tvm.TracklistName = pl.ListName;
            tvm.TracklistGUID = pl.GUID;

            SetPrevAndCurrShowingPlaylist(pl.ListName);
            RefreshPlaylistContent(pl.ListName, lb_PlaylistMenu.SelectedIndex);

            if (lv_Playlist.Visibility == Visibility.Hidden)
                lv_Playlist.Visibility = Visibility.Visible;
        }

        private void lb_PlaylistMenu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lb_PlaylistMenu.SelectedItems.Count == 1)
            {
                string selectedPlaylist = ((Playlist)lb_PlaylistMenu.SelectedItem).ListName;
                DeletePlaylistFromListBox(selectedPlaylist);
            }
        }

        private void DeletePlaylistFromListBox(string listName)
        {
            // Remove playist from database
            PlaylistCollection.RemovePlaylist(listName);
            // Refresh ListBoxItem
            OCPlaylist.Remove((Playlist)lb_PlaylistMenu.SelectedItem);

            // Switch content of lv_Playlist to previous one, or hide lv_Playlist if there is no remaining items in lb_PlaylistMenu
            if (lb_PlaylistMenu.Items.Count != 0)
            {
                listName = ((Playlist)lb_PlaylistMenu.Items[lb_PlaylistMenu.Items.Count - 1]).ListName;
                RefreshPlaylistContent(listName, lb_PlaylistMenu.Items.Count - 1);
            }
            else
            {
                lv_Playlist.Visibility = Visibility.Hidden;
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

        private void miRename_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;
            SwitchEditingMode(lbi, true);
        }

        private void miDelete_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;
            string listName = ((Playlist)lbi.DataContext).ListName;
            DeletePlaylistFromListBox(listName);
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

        private void lv_Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (lv_Playlist.DataContext as TracksViewModel).SelectedIndices = lv_Playlist.GetSelectedIndices();
        }
    }

    public class EmptyPlaylistException : Exception
    {
        public EmptyPlaylistException(string message) : base(message)
        {
        }
    }
}
