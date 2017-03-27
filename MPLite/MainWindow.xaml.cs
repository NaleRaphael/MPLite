using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace MPLite
{
    public partial class MainWindow : Window
    {
        // Window control
        private bool isWindowMaximized = false;

        // Pages
        private PagePlaylist pagePlaylist = null;
        private PageSetting pageSetting = null;
        private PageCalendar pageCalendar = null;

        // Menu_Setting
        private bool isMenuCollapsed = true;

        // Music player controls
        private readonly MusicPlayer _musicPlayer;
        public delegate TrackInfo GetTrackEventHandler(MusicPlayer player, string playlistName, int selectedIdx);
        public static event GetTrackEventHandler GetTrackEvent; // subscriber: MainWindow_GetTrackEvent @ PagePlaylist.xaml.cs
        public delegate void TrackIsPalyingEventHandler(TrackInfo track);
        public static event TrackIsPalyingEventHandler TrackIsPlayedEvent;  // subscriber: MainWindow_TrackIsPlayedEvent @ PagePlaylist.xaml.cs
        public delegate void TrackIsStoppedEventHandler(TrackInfo track);
        public static event TrackIsStoppedEventHandler TrackIsStoppedEvent; // subscriber: MainWindow_TrackIsStoppedEvent @ PagePlaylist.xaml.cs

        // Timer
        DispatcherTimer timer;

        // TrackBar control
        bool isScrolling = false;

        // Track status displayer module
        private TrackStatusDispModule trackStatusDisplayer;

        // Try to turn off navigation sound
        private const int Feature = 21; //FEATURE_DISABLE_NAVIGATION_SOUNDS
        private const int SetFeatureOnProcess = 0x00000002;
        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(int featureEntry,
            [MarshalAs(UnmanagedType.U4)] int dwFlags, bool fEnable);

        public MainWindow()
        {
            InitializeComponent();
            //CoInternetSetFeatureEnabled(Feature, SetFeatureOnProcess, true);
            URLSecurityZoneAPI.InternetSetFeatureEnabled(URLSecurityZoneAPI.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, URLSecurityZoneAPI.SetFeatureOn.PROCESS, true);

            // Page switcher
            PageSwitcher.pageSwitcher = this.Frame_PageSwitcher;

            // Menu_Setting
            Menu_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SPane_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;

            // Default page
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);

            // Music player
            _musicPlayer = new MusicPlayer();
            PagePlaylist.PlayTrackEvent += MainWindow_PlayTrackEvent;
            PagePlaylist.NewSelectionEvent += _musicPlayer.ResetQueue;
            _musicPlayer.PlayerStartedEvent += SetTimerAndTrackBar;
            _musicPlayer.PlayerStoppedEvent += ResetTimerAndTrackBar;
            _musicPlayer.PlayerPausedEvent += Set_btn_StartPlayback_Icon_Play;
            _musicPlayer.TrackEndsEvent += StopPlayerOrPlayNextTrack;

            // Timer control
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += Timer_Tick;

            // Track bar
            trackBar.IsMoveToPointEnabled = true;

            // Track status displayer
            trackStatusDisplayer = new TrackStatusDispModule(lbl_TrackProgess, lbl_TrackName);
            _musicPlayer.PlayerStartedEvent += trackStatusDisplayer.SetTrackName;
            _musicPlayer.PlayerStartedEvent += trackStatusDisplayer.SetTrackLength;
            _musicPlayer.PlayerStoppedEvent += trackStatusDisplayer.ResetTrackName;
            _musicPlayer.PlayerStoppedEvent += trackStatusDisplayer.ResetTrackProgress;

            // Scheduler
            PageCalendar.SchedulerIsTriggeredEvent += pagePlaylist.RunPlaylist;
        }

        #region PageControl
        private void PageSwitchControl<T>(ref T target) where T : Page, new()
        {
            if (target == null)
            {
                target = new T();
            }
            PageSwitcher.Switch(target);
        }

        private void Btn_Playlist_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);
        }

        private void Btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            CollapseMenuSetting(isMenuCollapsed);
        }

        private void MItem_Basic_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate to desired page
            PageSwitchControl<PageSetting>(ref pageSetting);
            CollapseMenuSetting(true);
        }

        private void MItem_Scheduler_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PageCalendar>(ref pageCalendar);
            CollapseMenuSetting(true);
        }

        private void btn_Setting_Basic_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate to desired page
            PageSwitchControl<PageSetting>(ref pageSetting);
            CollapseMenuSetting(true);
        }

        private void btn_Setting_Scheduler_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PageCalendar>(ref pageCalendar);
            CollapseMenuSetting(true);
        }
        #endregion

        #region MainWindow control
        private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isWindowMaximized)
            {
                isWindowMaximized = !isWindowMaximized;
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                isWindowMaximized = false;
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        private void Btn_ExitProgram_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CollapseMenuSetting(bool collapse)
        {
            isMenuCollapsed = !collapse;
            Menu_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SPane_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CloseProxyWindow()
        {
            //scheduler;
        }
        #endregion

        #region Music player control
        // Called from `PagePlaylist`, so that `selectedIdx` is avaliable.
        private void MainWindow_PlayTrackEvent(string playlistName, int selectedIdx)
        {
            PlayTrack(GetTrackEvent(_musicPlayer, playlistName, selectedIdx));
        }

        private void Btn_StartPlayback_Click(object sender, RoutedEventArgs e)
        {
            // If no track is selected, find the first track in "default playlist" (store this info in Property) and play it
            try
            {
                switch (_musicPlayer.PlayerStatus)
                {
                    case MusicPlayer.PlaybackState.Stopped:
                        // Call from MainWindow, so that player will start from the beginning of a list. (-1)
                        PlayTrack(GetTrackEvent(_musicPlayer, null, -1));
                        break;
                    case MusicPlayer.PlaybackState.Playing:
                        _musicPlayer.Pause();
                        break;
                    case MusicPlayer.PlaybackState.Paused:
                        _musicPlayer.Resume();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Beginning function to fire all events of playing track
        private void PlayTrack(TrackInfo trackInfo)
        {
            try
            {
                _musicPlayer.Stop();

                if (trackInfo == null)
                    return;
                _musicPlayer.Play(trackInfo);

                // Fire an event to notify LV_Playlist in Page_Playlist (change `playingSign` to ">")
                TrackIsPlayedEvent(trackInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                _musicPlayer.Stop();
            }
        }

        // Subscriber
        public void ResetTimerAndTrackBar(TrackInfo track)
        {
            timer.Stop();
            // Reset the posotion of thumb
            ResetTrackBar();

            // Reset icon of btn_StartPlayback
            Object obj = btn_StartPlayback.Template.FindName("content", btn_StartPlayback);
            ((ContentPresenter)obj).Content = FindResource("PlaybackCtrl_Play");

            // Fire event to notify subscribber that track has been stopped
            // (reset `TrackInfo.playingSign`, ...)
            /*if (_musicPlayer.PrevTrack != null)
                TrackIsStoppedEvent(_musicPlayer.PrevTrack);*/
            TrackIsStoppedEvent(_musicPlayer.PrevTrack);
        }

        // Subscriber
        private void SetTimerAndTrackBar(TrackInfo track)
        {
            trackBar.Maximum = _musicPlayer.GetSongLength();
            timer.Start();

            // Change icon of btn_StartPlayback to "Pause"
            Object obj = btn_StartPlayback.Template.FindName("content", btn_StartPlayback);
            ((ContentPresenter)obj).Content = FindResource("PlaybackCtrl_Pause");
        }

        // Subscriber
        private void Set_btn_StartPlayback_Icon_Play()
        {
            timer.Stop();
            Object obj = btn_StartPlayback.Template.FindName("content", btn_StartPlayback);
            ((ContentPresenter)obj).Content = FindResource("PlaybackCtrl_Play");
        }

        // Subscriber
        private void StopPlayerOrPlayNextTrack()
        {
            _musicPlayer.Stop();
            timer.Stop();

            // Play next track or replay the same track (according to user setting)
            PlayTrack(GetTrackEvent(_musicPlayer, null, -1));
        }
        #endregion

        #region Timer control
        private void Timer_Tick(object sender, EventArgs e)
        {
            int temp = 0;
            if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Playing)
            {
                if (isScrolling)
                {
                    return;
                }
                try
                {
                    temp = _musicPlayer.GetCurrentMilisecond();
                    trackBar.Value = temp;
                    trackStatusDisplayer.SetTrackProgress(temp);
                }
                catch (Exception ex)
                {
                    _musicPlayer.Stop();
                    MessageBox.Show(ex.Message);
                    MessageBox.Show(ex.StackTrace);
                }
            }
            else if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Paused)
            {
                timer.Stop();
                // Next sound / Stop (set by config -> playback type: single track, list ... )
            }
            else if (_musicPlayer.CurrentTrack == null)
            {
                timer.Stop();
            }
        }
        #endregion

        #region TrackBar control
        private void Slider_DragCompleted(object sender, EventArgs e)
        {
            //MessageBox.Show(trackBar.Value.ToString());
            // TODO
        }

        private void ResetTrackBar()
        {
            trackBar.Value = 0;
        }

        private void trackBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Stopped)
                return;
            isScrolling = true;
            timer.Stop();
        }

        private void trackBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Stopped)
                return;
            isScrolling = false;
            _musicPlayer.SetPosition((int)trackBar.Value);

            // Set track progress
            trackStatusDisplayer.SetTrackProgress((int)trackBar.Value);
            if (_musicPlayer.PlayerStatus != MusicPlayer.PlaybackState.Paused)
                timer.Start();
        }
        #endregion

        #region Status displayer control
        #endregion

        #region DragMove
        private void DPane_Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Workaround: extend the draggable region of window
        private void lbl_TrackName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Workaround: extend the draggable region of window
        private void lbl_TrackProgess_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        #endregion

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Space)
            {
                if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Playing)
                {
                    _musicPlayer.Pause();
                }
                else if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Paused)
                {
                    _musicPlayer.Resume();
                }
            }
        }
    }
}
