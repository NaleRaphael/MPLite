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
        private bool hasEnteredVolumeBar = false;

        // Pages
        private PagePlaylist pagePlaylist = null;
        private PageSetting pageSetting = null;
        private PageCalendar pageCalendar = null;

        // Menu_Setting
        private bool isMenuCollapsed = true;

        // Music player controls
        private readonly MusicPlayer _musicPlayer;
        public delegate PlayTrackEventArgs GetTrackEventHandler(MusicPlayer player, string selectedPlaylist = null, 
            int selectedTrackIndex = -1, MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None);
        public static event GetTrackEventHandler GetTrackEvent; // subscriber: MainWindow_GetTrackEvent @ PagePlaylist.xaml.cs
        public delegate void TrackIsPalyingEventHandler(PlayTrackEventArgs e);
        public static event TrackIsPalyingEventHandler TrackIsPlayedEvent;  // subscriber: MainWindow_TrackIsPlayedEvent @ PagePlaylist.xaml.cs
        public delegate void TrackIsStoppedEventHandler(PlayTrackEventArgs e);
        public static event TrackIsStoppedEventHandler TrackIsStoppedEvent; // subscriber: MainWindow_TrackIsStoppedEvent @ PagePlaylist.xaml.cs
        public delegate void FailedToPlayTrackEventHandler(PlayTrackEventArgs e);
        public static event FailedToPlayTrackEventHandler FailedToPlayTrackEvent;

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
            SPane_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;

            // Default page
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);

            // Music player
            _musicPlayer = new MusicPlayer();
            PagePlaylist.PlayTrackEvent += MainWindow_PlayTrackEvent;
            PagePlaylist.NewSelectionEvent += _musicPlayer.ClearQueue;
            PagePlaylist.StopPlayerRequestEvent += _musicPlayer.Stop;
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

            // Volume bar
            trackbarVolume.Visibility = Visibility.Hidden;
            trackbarVolume.Maximum = 1000;  // limit: 1000 (mciSendString@winmm.dll)
            trackbarVolume.Value = Properties.Settings.Default.Volume;

            // Volume button
            //SetVolumeIcon(Properties.Settings.Default.Volume, Properties.Settings.Default.IsMuted);

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

        private void btn_Setting_Basic_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate to desired page
            PageSwitchControl<PageSetting>(ref pageSetting);
            CollapseMenuSetting(false);

        }

        private void btn_Setting_Scheduler_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PageCalendar>(ref pageCalendar);
            CollapseMenuSetting(false);
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

        private void CollapseMenuSetting(bool isCollapsed)
        {
            isMenuCollapsed = !isCollapsed;
            SPane_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CloseProxyWindow()
        {
            //scheduler;
        }
        #endregion

        #region Music player control
        private void MainWindow_PlayTrackEvent(string selectedPlaylist = null, int selectedTrackIndex = -1,
            MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None)
        {
            PlayTrack(GetTrackEvent(_musicPlayer, selectedPlaylist, selectedTrackIndex, mode));
        }

        private void btnStartPlayback_Click(object sender, RoutedEventArgs e)
        {
            // If no track is selected, find the first track in "default playlist" (store this info in Property) and play it
            try
            {
                switch (_musicPlayer.PlayerStatus)
                {
                    case MusicPlayer.PlaybackState.Stopped:
                        // Call from MainWindow, so that player will start from the beginning of a list. (-1)
                        PlayTrack(GetTrackEvent(_musicPlayer));
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
        private void PlayTrack(PlayTrackEventArgs e)
        {
            try
            {
                _musicPlayer.Stop(e);    // TODO: pass a PlayTrackEventArgs into it

                if (e.CurrTrack == null)
                    return;
                _musicPlayer.Play(e);

                // Fire an event to notify LV_Playlist in Page_Playlist (change `playingSign` to ">")
                TrackIsPlayedEvent(e);
            }
            catch (InvalidFilePathException ex_InvaildPath)
            {
                // Set status of the problematic track (!)
                FailedToPlayTrackEvent(e);

                // TODO: get next track
                StopPlayerOrPlayNextTrack(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                _musicPlayer.Stop(e);
            }
        }

        // Subscriber
        public void ResetTimerAndTrackBar(PlayTrackEventArgs e)
        {
            timer.Stop();
            // Reset the posotion of thumb
            ResetTrackBar();

            // Reset icon of btn_StartPlayback
            Object obj = btnStartPlayback.Template.FindName("content", btnStartPlayback);
            ((ContentPresenter)obj).Content = FindResource("PlaybackCtrl_Play");

            // Fire event to notify subscribber that track has been stopped
            // (reset `TrackInfo.playingSign`, ... etc)
            TrackIsStoppedEvent(e);
        }

        // Subscriber
        private void SetTimerAndTrackBar(PlayTrackEventArgs e)
        {
            trackBar.Maximum = _musicPlayer.GetSongLength();
            timer.Start();

            // Change icon of btn_StartPlayback to "Pause"
            Object obj = btnStartPlayback.Template.FindName("content", btnStartPlayback);
            ((ContentPresenter)obj).Content = FindResource("PlaybackCtrl_Pause");
        }

        // Subscriber
        private void Set_btn_StartPlayback_Icon_Play()
        {
            timer.Stop();
            Object obj = btnStartPlayback.Template.FindName("content", btnStartPlayback);
            ((ContentPresenter)obj).Content = FindResource("PlaybackCtrl_Play");
        }

        // Subscriber
        private void StopPlayerOrPlayNextTrack(PlayTrackEventArgs e)
        {
            // Play next track or replay the same track (according to user setting)
            PlayTrack(GetTrackEvent(_musicPlayer, e.PlaylistName, e.CurrTrackIndex, e.PlaybackMode));
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
                    _musicPlayer.Stop(null);
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

        #region Volume control
        private void ShowOrHideVolumeBar(bool show)
        {
            trackbarVolume.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            btnBackward.Visibility = show ? Visibility.Hidden : Visibility.Visible;
            btnStartPlayback.Visibility = show ? Visibility.Hidden : Visibility.Visible;
            btnForward.Visibility = show ? Visibility.Hidden : Visibility.Visible;
        }

        private void btnVolumeControl_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowOrHideVolumeBar(!(trackbarVolume.Visibility == Visibility.Visible));
        }

        private void btnVolumeControl_MouseLeave(object sender, MouseEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Start();
            timer.Tick += (tSender, tArgs) =>
            {
                timer.Stop();
                if (!hasEnteredVolumeBar)
                    ShowOrHideVolumeBar(hasEnteredVolumeBar);
            };
        }

        private void trackbarVolume_MouseEnter(object sender, MouseEventArgs e)
        {
            hasEnteredVolumeBar = true;
        }

        private void trackbarVolume_MouseLeave(object sender, MouseEventArgs e)
        {
            hasEnteredVolumeBar = false;
            ShowOrHideVolumeBar(false);
        }

        private void trackbarVolume_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _musicPlayer.SetVolume((int)trackbarVolume.Value);

            // TODO: change icon according volume
            SetVolumeIcon((int)trackbarVolume.Value, Properties.Settings.Default.IsMuted);

            Properties.Settings.Default.Volume = (int)trackbarVolume.Value;
            Properties.Settings.Default.Save();
        }

        private void SetVolumeIcon(double value, bool isMuted)
        {
            Object obj = btnVolumeControl.Template.FindName("content", btnVolumeControl);

            if (isMuted)
            {
                ((ContentPresenter)obj).Content = FindResource("Volume_muted");
            }
            else if (value > 1000*2/3.0)
            {
                ((ContentPresenter)obj).Content = FindResource("Volume_100");
            }
            else if (value > 1000/3.0)
            {
                ((ContentPresenter)obj).Content = FindResource("Volume_66");
            }
            else if (value > 0)
            {
                ((ContentPresenter)obj).Content = FindResource("Volume_33");
            }
            else
            {
                ((ContentPresenter)obj).Content = FindResource("Volume_0");
            }
        }
        #endregion

        private void btnVolumeControl_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Properties.Settings.Default.IsMuted = !Properties.Settings.Default.IsMuted;
            Properties.Settings.Default.Save();

            int volume = (Properties.Settings.Default.IsMuted) ? 0 : Properties.Settings.Default.Volume;
            _musicPlayer.SetVolume(volume);

            // Change icon of btnVolumeControl
            SetVolumeIcon(Properties.Settings.Default.Volume, Properties.Settings.Default.IsMuted);
        }
    }
}
