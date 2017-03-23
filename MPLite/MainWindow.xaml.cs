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
        public delegate TrackInfo GetTrackEventHandler();
        public static event GetTrackEventHandler GetTrackEvent;
        public delegate void TrackIsPalyingEventHandler(TrackInfo track);
        public static event TrackIsPalyingEventHandler TrackIsPlayedEvent;
        public delegate void TrackIsStoppedEventHandler(TrackInfo track);
        public static event TrackIsStoppedEventHandler TrackIsStoppedEvent;

        // Timer
        DispatcherTimer timer;

        // TrackBar control
        bool isScrolling = false;

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

            // Default page
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);

            // Music player
            _musicPlayer = new MusicPlayer();
            PagePlaylist.PlayTrackEvent += MainWindow_PlayTrackEvent;
            _musicPlayer.PlayerStoppedEvent += _musicPlayer_PlayerStoppedEvent;
            _musicPlayer.PlayerStartedEvent += _musicPlayer_PlayerStartedEvent;
            _musicPlayer.PlayerPausedEvent += _musicPlayer_PlayerPausedEvent;
            _musicPlayer.TrackEndsEvent += _musicPlayer_TrackEndsEvent;

            // Timer control
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += Timer_Tick;

            // Track bar
            trackBar.IsMoveToPointEnabled = true;

            
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
            CollapseMenuSetting(false);
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
        #endregion

        #region MainWindow control
        private void DPane_Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isWindowMaximized)
            {
                isWindowMaximized = !isWindowMaximized;
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
            else {
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
            Menu_Setting.Visibility = collapse ? Visibility.Collapsed : Visibility.Visible;
            isMenuCollapsed = collapse;
        }

        private void CloseProxyWindow()
        {
            //scheduler;
        }
        #endregion

        #region Music player control
        private void MainWindow_PlayTrackEvent(TrackInfo trackInfo)
        {
            PlayTrack(trackInfo);
        }

        private void Btn_StartPlayback_Click(object sender, RoutedEventArgs e)
        {
            // TODO: play music
            // TODO: show status of playback
            // If no track is selected, find the first track in "default playlist" (store this info in Property) and play it
            try
            {
                switch (_musicPlayer.PlayerStatus)
                {
                    case MusicPlayer.PlaybackState.Stopped:
                        PlayTrack(GetTrackEvent());
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

        private void PlayTrack(TrackInfo trackInfo)
        {
            try
            {
                _musicPlayer.Stop();
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
        private void _musicPlayer_PlayerStoppedEvent()
        {
            timer.Stop();
            // Reset the posotion of thumb
            ResetTrackBar();

            // Reset icon of btn_StartPlayback
            tBtn_StartPlayback.Content = FindResource("PlaybackCtrl_Play");

            // Fire event to notify subscribber that track has been stopped
            // (reset `TrackInfo.playingSign`, ...)
            if (_musicPlayer.PrevTrack != null)
                TrackIsStoppedEvent(_musicPlayer.PrevTrack);
        }

        // Subscriber
        private void _musicPlayer_PlayerStartedEvent()
        {
            trackBar.Maximum = _musicPlayer.GetSongLength();
            timer.Start();

            // Change icon of btn_StartPlayback to "Pause"
            tBtn_StartPlayback.Content = FindResource("PlaybackCtrl_Pause");
        }

        // Subscriber
        private void _musicPlayer_PlayerPausedEvent()
        {
            timer.Stop();
        }

        // Subscriber
        private void _musicPlayer_TrackEndsEvent()
        {
            _musicPlayer.Stop();
            timer.Stop();
            
            // Play next track or replay the same track (according to user setting)
        }
        #endregion

        #region Timer control
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_musicPlayer.PlayerStatus == MusicPlayer.PlaybackState.Playing)
            {
                if (isScrolling)
                {
                    return;
                }
                try
                {
                    // add label for showing current time
                    trackBar.Value = _musicPlayer.GetCurrentMilisecond();
                }
                catch (Exception ex)
                {
                    _musicPlayer.Stop();
                    MessageBox.Show(ex.Message);
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
            
        }
        private void ResetTrackBar()
        {
            trackBar.Value = 0;
        }
        #endregion

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
            timer.Start();
        }

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
