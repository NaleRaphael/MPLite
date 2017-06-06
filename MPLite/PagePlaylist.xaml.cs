using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MPLite
{
    using TrackInfo = Core.TrackInfo;
    using TrackStatus = Core.TrackStatus;
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
    using SchedulerEventArgs = Event.SchedulerEventArgs;
    using PlaybackMode = Core.PlaybackMode;
    using PlaybackCommands = Event.PlaybackCommands;

    public partial class PagePlaylist : Page
    {
        #region Event
        public delegate void PlayTrackEventHandler(Guid playlistGUID, int selectedTrackIndex = -1, PlaybackMode mode = PlaybackMode.None);
        public static event PlayTrackEventHandler PlayTrackEvent;
        public delegate TrackStatusEventArgs ListContentIsRefreshedEventHandler();
        public static event ListContentIsRefreshedEventHandler ListContentIsRefreshedEvent;
        public delegate void PlaylistIsUpdatedEventHandler(UpdatePlaylistEventArgs e);
        public static event PlaylistIsUpdatedEventHandler PlaylistIsUpdatedEvent;

        public delegate void StopPlayerRequestEventHandler();
        public static event StopPlayerRequestEventHandler StopPlayerRequestEvent;
        public delegate void PausePlayerRequestEventHandler();
        public static event PausePlayerRequestEventHandler PausePlayerRequestEvent;

        #endregion

        #region Field
        private string oriPlaylistName = "";
        #endregion

        public PagePlaylist()
        {
            InitializeComponent();
            InitPlaylist();

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
            trackListsVM.CurrentPlaylist = PlaylistCollection.GetPlaylist(Properties.Settings.Default.LastSelectedPlaylistGUID);
            lb_PlaylistMenu.DataContext = trackListsVM;
            lb_PlaylistMenu.ItemsSource = trackListsVM.TrackLists;
        }

        private void InitializePlaylistContent()
        {
            TracksViewModel tracksVM = new TracksViewModel(PlaylistCollection.GetPlaylist(Properties.Settings.Default.LastSelectedPlaylistGUID));
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null)
                throw new Exception("Failed to cast object");

            lv_Playlist.DataContext = tracksVM;
            if (trackListsVM.CurrentPlaylist != null)
            {
                lv_Playlist.ItemsSource = trackListsVM.CurrentPlaylist.Soundtracks as IEnumerable<TrackInfo>;
                tracksVM.UpdateSoundtracks(trackListsVM.CurrentPlaylist);
            }
            else
            {
                lv_Playlist.ItemsSource = null;
            }

            // Bind event for updating content of view-model of lb_PlaylistMenu
            tracksVM.PlaylistIsUpdatedEvent += (lb_PlaylistMenu.DataContext as TrackListsViewModel).UpdateTrackList;
            // Update trackQueue
            tracksVM.PlaylistIsUpdatedEvent += OnPlaylistIsUpdated;
        }

        private void RefreshPlaylist()
        {
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null) return;

            Playlist pl = null;

            trackListsVM.UpdateTrackLists(plc.TrackLists);
            if (trackListsVM.CurrentPlaylist != null)
                pl = trackListsVM.TrackLists.FirstOrDefault(x => x.GUID == trackListsVM.CurrentPlaylist.GUID);
            lb_PlaylistMenu.SelectedItem = pl;
        }

        private void RefreshPlaylistContent()
        {
            TracksViewModel tracksVM = lv_Playlist.DataContext as TracksViewModel;
            if (tracksVM == null) return;
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null) return;

            Playlist pl = trackListsVM.CurrentPlaylist;
            if (pl == null)
            {
                lv_Playlist.Visibility = Visibility.Hidden;
                return;
            }

            tracksVM.UpdatePlaylistInfo(pl);
            tracksVM.UpdateSoundtracks(pl);
            lv_Playlist.ItemsSource = tracksVM.Soundtracks as IEnumerable<TrackInfo>;

            // Update info
            Properties.Settings.Default.LastSelectedPlaylistGUID = pl.GUID;
            Properties.Settings.Default.Save();

            // Show playing track
            if (lb_PlaylistMenu.SelectedItem != null && ListContentIsRefreshedEvent != null)
            {
                if (Properties.Settings.Default.TaskPlaylistGUID == pl.GUID)
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
            Playlist pl = trackListsVM.TrackLists.First(x => x.GUID == e.OwnerListGUID);
            pl.Soundtracks.Find(x => x.GUID == e.Track.GUID).TrackStatus = e.Track.TrackStatus;

            if (pl.GUID == e.OwnerListGUID)
                tracksVM.UpdateTrackStatus(e);
        }
        #endregion

        #region lv_Playlist controls
        private void lv_Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int selIdx = lv_Playlist.SelectedIndex;
            if (selIdx < 0) return;

            Guid listGUID = ((Playlist)lb_PlaylistMenu.SelectedItem).GUID;

            StopPlayerRequestEvent();

            Properties.Settings.Default.TaskPlaybackMode = Properties.Settings.Default.PlaybackMode;
            Properties.Settings.Default.TaskPlaylistGUID = listGUID;
            Properties.Settings.Default.Save();
            PlayTrackEvent(listGUID, selIdx, (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode);
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
                        StopPlayerRequestEvent();
                    }
                }

                tracksVM.RemoveTracks();
            }
        }

        private void lv_Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (lv_Playlist.DataContext as TracksViewModel).SelectedIndices = lv_Playlist.GetSelectedIndices();
        }
        #endregion

        private void PlaySoundtrack(Guid playlistGUID, int selectedTrackIndex = -1,
            PlaybackMode mode = PlaybackMode.None)
        {
            try
            {
                // Fire event to notify MainWindow that a track is selected and waiting to be played
                PlayTrackEvent(playlistGUID, selectedTrackIndex, mode);
            }
            catch
            {
                throw;
            }
        }

        // TODO: add a parameter: get prev/next
        private TrackInfo GetTrack(MusicPlayer player, Guid listGUID,
            int selectedTrackIndex = -1, PlaybackMode mode = PlaybackMode.None, bool selectNext = true)
        {
            if (lb_PlaylistMenu.Items.Count == 0)
            {
                throw new EmptyPlaylistException("No tracklist is avalible");
            }

            try
            {
                // If no playlist is selected (user click btn_StartPlayback to play music)
                TracksViewModel tracksVM = lv_Playlist.DataContext as TracksViewModel;
                listGUID = (listGUID == Guid.Empty) ? tracksVM.TracklistGUID : listGUID;

                if (mode == PlaybackMode.None)
                    throw new Exception("Invalid playback mode");

                return selectNext ? player.GetTrack(listGUID, selectedTrackIndex, mode) : player.GetPrevTrack(listGUID, selectedTrackIndex, mode);
            }
            catch
            {
                throw;
            }
        }

        private void OnPlaylistIsUpdated(UpdatePlaylistEventArgs e)
        {
            PlaylistIsUpdatedEvent(e);
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
            TrackListsViewModel trackListsVM = lb_PlaylistMenu.DataContext as TrackListsViewModel;
            if (trackListsVM == null) return;
            if (trackListsVM.CurrentPlaylist != null && pl.GUID == trackListsVM.CurrentPlaylist.GUID) return;

            trackListsVM.CurrentPlaylist = pl;

            RefreshPlaylistContent();

            if (lv_Playlist.Visibility == Visibility.Hidden)
                lv_Playlist.Visibility = Visibility.Visible;
        }

        private void lb_PlaylistMenu_KeyDown(object sender, KeyEventArgs e)
        {
            // Remove this functionality to avoid user deleting playlist by pressing delete key directly
#if false
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
#endif
        }

        private void MenuListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)    // Rename listName in the ListBoxItem
            {
                MPLiteSetting.IsEditing = true;
                SwitchEditingMode((ListBoxItem)sender, true);
                oriPlaylistName = (lb_PlaylistMenu.SelectedItem as Playlist).ListName;
            }
            else if (e.Key == Key.Escape)   // Disable editable mode of selected TextBoxItem
            {
                TextBox txtBox = ((ListBoxItem)sender).Template.FindName("txtEditBox", (FrameworkElement)sender) as TextBox;
                if (txtBox.IsFocused)
                    lb_PlaylistMenu.Focus();    // Transfer focus
                oriPlaylistName = "";   // Reset
                MPLiteSetting.IsEditing = false;
            }
            else if (e.Key == Key.Enter)    // User confirms the new name
            {
                TextBox txtBox = ((ListBoxItem)sender).Template.FindName("txtEditBox", (FrameworkElement)sender) as TextBox;
                txtEditBox_LostFocus(txtBox, new RoutedEventArgs());    // Trigger LostFocus event to save change
                MPLiteSetting.IsEditing = false;
            }
            else if (e.Key == Key.Up)
            {
                if (lb_PlaylistMenu.SelectedIndex <= 0) return;
                lb_PlaylistMenu.SelectedIndex -= 1;
                lb_PlaylistMenu_SelectionChanged(null, null);
            }
            else if (e.Key == Key.Down)
            {
                if (lb_PlaylistMenu.SelectedIndex >= lb_PlaylistMenu.Items.Count - 1) return;
                lb_PlaylistMenu.SelectedIndex += 1;
                lb_PlaylistMenu_SelectionChanged(null, null);
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
                tracksVM.UpdatePlaylistInfo(targetGUID, result);
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
                    break;
                case PlaybackCommands.Play:
                    StopPlayerRequestEvent();
                    PlaySoundtrack(e.PlaylistGUID, e.TrackIndex, e.Mode);
                    break;
                default:
                    // Add a handling process for this exception
                    throw new Exception("Invalid command");
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
