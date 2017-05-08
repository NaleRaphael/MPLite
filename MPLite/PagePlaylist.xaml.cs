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
    using TrackStatus = Core.TrackStatus;
    using MPLiteConstant = Core.MPLiteConstant;
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
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

            //lb_PlaylistMenu.SelectedIndex = Properties.Settings.Default.LastSelectedPlaylistIndex;  // select default list
            currShowingPlaylist = Properties.Settings.Default.LastSelectedPlaylist;

            MainWindow.GetTrackEvent += GetTrack;
        }

        #region Initialization
        private void InitPlaylist()
        {
            try
            {
                InitializePlaylistMenu();
                InitializePlaylistContent();

                RefreshPlaylist();
                RefreshPlaylistContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void InitializePlaylistMenu()
        {
            TrackListsViewModel trackListsVM = new TrackListsViewModel();
            trackListsVM.CurrentPlaylist = PlaylistCollection.GetPlaylist(Properties.Settings.Default.LastSelectedPlaylist);
            lb_PlaylistMenu.DataContext = trackListsVM;
            lb_PlaylistMenu.ItemsSource = trackListsVM.TrackLists;
        }

        private void InitializePlaylistContent()
        {
            TracksViewModel tracksVM = new TracksViewModel(PlaylistCollection.GetPlaylist(Properties.Settings.Default.LastSelectedPlaylist));
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null)
                throw new Exception("No avalible playlist.");

            lv_Playlist.DataContext = tracksVM;
            if (trackListsVM.CurrentPlaylist != null)
            {
                lv_Playlist.ItemsSource = trackListsVM.CurrentPlaylist.Soundtracks as IEnumerable<TrackInfo>;
                tracksVM.UpdateSoundtracks(trackListsVM.CurrentPlaylist);
            }
            else
                lv_Playlist.ItemsSource = null;

            // Bind event for updating content of view-model of lb_PlaylistMenu
            tracksVM.PlaylistIsUpdatedEvent += (lb_PlaylistMenu.DataContext as TrackListsViewModel).UpdateTrackList;
            //tracksVM.PlaylistIsUpdatedEvent += SetTrackStatus.
        }

        private void RefreshPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null) return;

            trackListsVM.UpdateTrackLists(plc.TrackLists);
            lb_PlaylistMenu.SelectedItem = trackListsVM.TrackLists.First(x => x.GUID == trackListsVM.CurrentPlaylist.GUID);
        }

        private void RefreshPlaylistContent()
        {
            TracksViewModel tracksVM = lv_Playlist.DataContext as TracksViewModel;
            if (tracksVM == null) return;
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null) return;

            Playlist pl = trackListsVM.CurrentPlaylist;
            if (pl == null) return;

            tracksVM.UpdateSoundtracks(pl);
            lv_Playlist.ItemsSource = tracksVM.Soundtracks as IEnumerable<TrackInfo>;

            // Update info
            Properties.Settings.Default.LastSelectedPlaylist = pl.ListName;
            Properties.Settings.Default.Save();

            // Show playing track
            if (lb_PlaylistMenu.SelectedItem != null && ListContentIsRefreshedEvent != null)
            {
                if (Properties.Settings.Default.TaskPlaylist == pl.ListName)
                {
                    tracksVM.UpdateTrackStatus(ListContentIsRefreshedEvent());
                }
            }
        }
        #endregion

        #region Track status control
        public void SetTrackStatus(TrackStatusEventArgs e)
        {
            // Check
            if (e == null || e.Index == -1)
                return;

            TracksViewModel tracksVM = lv_Playlist.DataContext as TracksViewModel;
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;

            // TODO: try to rewrite this
            Playlist pl = trackListsVM.TrackLists.First(x => x.ListName == e.OwnerList);
            pl.Soundtracks.Find(x => x.GUID == e.Track.GUID).TrackStatus = e.Track.TrackStatus;

            if (pl.ListName == e.OwnerList)
                tracksVM.UpdateTrackStatus(e);
        }
        #endregion

        #region lv_Playlist controls
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
                TracksViewModel tracksVM = lv_Playlist.DataContext as TracksViewModel;
                List<TrackInfo> tracks = lv_Playlist.SelectedItems.OfType<TrackInfo>().ToList();

                // Stop player if selected track is playing
                foreach (TrackInfo track in lv_Playlist.SelectedItems)
                {
                    if (track.TrackStatus == TrackStatus.Playing)
                    {
                        StopPlayerRequestEvent();  // TODO: update trackStatus
                    }
                }

                tracksVM.RemoveTracksByIndices();
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
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null)
                throw new Exception("Empty content of lb_PlaylistMenu");

            Playlist pl = trackListsVM.CreatePlaylist();
            lb_PlaylistMenu.SelectedItem = pl;
            RefreshPlaylistContent();
            
            if (lv_Playlist.Visibility == Visibility.Hidden)
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
            if (pl == null) return;
            TrackListsViewModel trackListVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListVM == null) return;

            if (pl.ListName == trackListVM.CurrentPlaylist.ListName) return;

            trackListVM.CurrentPlaylist = pl;

            RefreshPlaylistContent();

            if (lv_Playlist.Visibility == Visibility.Hidden)
                lv_Playlist.Visibility = Visibility.Visible;
        }

        private void lb_PlaylistMenu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lb_PlaylistMenu.SelectedItems.Count == 1)
            {
                TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
                Playlist pl = lb_PlaylistMenu.SelectedItem as Playlist;
                if (pl == null)
                    throw new Exception("Failed to cast selected item to Playlist");

                trackListsVM.RemovePlaylist(pl.GUID);
                lb_PlaylistMenu.SelectedIndex = trackListsVM.TrackLists.Count - 1;
                RefreshPlaylistContent();
            }
        }

        private void MenuListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)    // Rename listName in the ListBoxItem
            {
                SwitchEditingMode((ListBoxItem)sender, true);
                oriPlaylistName = (lb_PlaylistMenu.SelectedItem as Playlist).ListName;
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

            Guid targetGUID = (lb_PlaylistMenu.SelectedItem as Playlist).GUID;
            if (result != oriPlaylistName)
            {
                TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
                trackListsVM.UpdatePlaylistName(targetGUID, result);
                TracksViewModel tracksVM = lv_Playlist.DataContext as TracksViewModel;
                tracksVM.UpdatePlaylistName(result);
                Properties.Settings.Default.LastSelectedPlaylist = result;
                Properties.Settings.Default.Save();
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
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            Playlist pl = lb_PlaylistMenu.SelectedItem as Playlist;
            if (pl == null)
                throw new Exception("Failed to cast selected item to Playlist");

            trackListsVM.RemovePlaylist(pl.GUID);
            lb_PlaylistMenu.SelectedIndex = trackListsVM.TrackLists.Count - 1;
            RefreshPlaylistContent();
        }
        #endregion

        #region Handle events fired from scheduler
        // TODO: rename this
        public void RunPlaylist(SchedulerEventArgs e)
        {
            switch (e.Command)
            {
                case PlaybackCommands.Pause:
                    PausePlayerRequestEvent();
                    break;
                case PlaybackCommands.Stop:
                    StopPlayerRequestEvent();
                    NewSelectionEvent();    // Notify MusicPlayer to reset queue
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
