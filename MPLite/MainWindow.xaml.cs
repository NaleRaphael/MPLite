using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

#if DEBUG
using System.IO;
#endif

namespace MPLite
{
    using PlayTrackEventArgs = Core.PlayTrackEventArgs;
    using PlaybackMode = Core.PlaybackMode;
    using TrackInfo = Core.TrackInfo;

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
        private bool hasEnteredMenuSetting = false;
        private bool hasEnteredBtnSetting = false;
        private bool isMenuSettingShowing = false;

        // Music player controls
        private readonly MusicPlayer _musicPlayer;

        public delegate TrackInfo GetTrackEventHandler(MusicPlayer player, Guid playlistGUID, 
            int selTrackIdx = -1, PlaybackMode mode = PlaybackMode.None, bool selectNext = true);
        public static event GetTrackEventHandler GetTrackEvent;
        public delegate void TrackIsPalyingEventHandler(PlayTrackEventArgs e);
        
        // Timer (tracing track progress)
        private DispatcherTimer timer;
        private DispatcherTimer volTimer;

        // Animation control
        private bool doesAnimationEnd = true;

        // TrackBar control
        private bool isScrolling = false;

        // Track status displayer module
        private TrackStatusDispModule trackStatusDisplayer;

        // Hotkeys
        private Hotkeys MPLiteHotkeys;

#if DEBUG
        FileStream ostrm;
        StreamWriter writer;
#endif

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();

            //CoInternetSetFeatureEnabled(Feature, SetFeatureOnProcess, true);
            URLSecurityZoneAPI.InternetSetFeatureEnabled(URLSecurityZoneAPI.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, URLSecurityZoneAPI.SetFeatureOn.PROCESS, true);

            // Page switcher
            PageSwitcher.pageSwitcher = this.framePageSwitcher;

            // Menu_Setting (footer)
            ShowMenuSetting(false);

            // Music player
            _musicPlayer = new MusicPlayer();
            PagePlaylist.PlayTrackEvent += this.PlayTrackFromPageList;
            PagePlaylist.StopPlayerRequestEvent += _musicPlayer.Stop;
            PagePlaylist.StopPlayerRequestEvent += _musicPlayer.ClearQueue;
            PagePlaylist.PausePlayerRequestEvent += _musicPlayer.Pause;
            PagePlaylist.PlaylistIsUpdatedEvent += _musicPlayer.UpdateQueue;

            _musicPlayer.PlayerStartedEvent += this.SetTimerAndTrackBar;
            _musicPlayer.PlayerStoppedEvent += this.ResetTimerAndTrackBar;
            _musicPlayer.PlayerPausedEvent += this.PlayerPauseHandler;
            _musicPlayer.TrackEndsEvent += this.StopOrPlayNextTrack;

            // Timer control
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += Timer_Tick;

            // Track bar
            trackBar.IsMoveToPointEnabled = true;
            trackBar.Visibility = Visibility.Hidden;

            // Volume bar
            volumeBar.Visibility = Visibility.Hidden;
            volumeBar.Maximum = 1000;  // limit: 1000 (mciSendString@winmm.dll)
            volumeBar.Value = Properties.Settings.Default.Volume;

            // Volume button
            SetVolumeIcon(Properties.Settings.Default.Volume, Properties.Settings.Default.IsMuted);

            // Volume control
            volTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            volTimer.Tick += SetPlayerVolume;

            // Track status displayer
            trackStatusDisplayer = new TrackStatusDispModule(lbl_TrackProgess, lbl_TrackName, trackBar);
            _musicPlayer.PlayerStartedEvent += trackStatusDisplayer.SetTrackName;
            _musicPlayer.PlayerStartedEvent += trackStatusDisplayer.SetTrackLength;
            _musicPlayer.PlayerStartedEvent += trackStatusDisplayer.ShowTrackBar;
            _musicPlayer.PlayerStoppedEvent += trackStatusDisplayer.ResetTrackName;
            _musicPlayer.PlayerStoppedEvent += trackStatusDisplayer.ResetTrackProgress;
            _musicPlayer.PlayerStoppedEvent += trackStatusDisplayer.HideTrackBar;

            // Set default page
            // NOTE: PagePlaylist have to be created early than PageCalendar, so that PageCalendar.SchedulerEvent can be assigned.
            pagePlaylist = new PagePlaylist();
            PageCalendar.SchedulerEvent += pagePlaylist.RunPlaylist;
            PageCalendar.Owner = this;
            pageCalendar = new PageCalendar();      // workaround: create PageCalendar to load EventManager
            
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);

            pageSetting = new PageSetting();
            this.MPLiteHotkeys = PageSetting.MPLiteHotKeys;

            _musicPlayer.PlayerStartedEvent += pagePlaylist.SetTrackStatus;
            _musicPlayer.PlayerStoppedEvent += pagePlaylist.SetTrackStatus;

            PagePlaylist.ListContentIsRefreshedEvent += this.GetPlayingTrackStatus;

            UpdateLayout();

#if DEBUG
            try
            {
                ostrm = new FileStream("./MPLiteLog.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now);
                Console.WriteLine("Cannot open MPLiteLog.txt for writting.");
                Console.WriteLine(ex.Message);
                return;
            }

            Console.SetOut(writer);
#endif
        }

        private void InitializeWindow()
        {
            this.Height = Properties.Settings.Default.WindowHeight;
            this.Width = Properties.Settings.Default.WindowWidth;
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

        private void btnPlaylist_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);
        }

        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            ShowMenuSetting(!isMenuSettingShowing);
        }

        private void btnSetting_MouseEnter(object sender, MouseEventArgs e)
        {
            hasEnteredBtnSetting = true;
            if (!isMenuSettingShowing)
            {
                ShowMenuSetting(true);
            }
        }

        private void btnSetting_MouseLeave(object sender, MouseEventArgs e)
        {
            DispatcherTimer tempTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
            tempTimer.Start();
            tempTimer.Tick += (tSender, tArgs) =>
            {
                tempTimer.Stop();
                if (!hasEnteredMenuSetting && isMenuSettingShowing)
                {
                    ShowMenuSetting(false);
                }
                hasEnteredBtnSetting = false;
                tempTimer = null;
            };
        }

        private void sPanelSetting_MouseEnter(object sender, MouseEventArgs e)
        {
            if (doesAnimationEnd)
                hasEnteredMenuSetting = true;
        }

        private void sPanelSetting_MouseLeave(object sender, MouseEventArgs e)
        {
            DispatcherTimer tempTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            tempTimer.Start();
            tempTimer.Tick += (tSender, tArgs) =>
            {
                tempTimer.Stop();
                if (hasEnteredMenuSetting && isMenuSettingShowing && !hasEnteredBtnSetting)
                {
                    ShowMenuSetting(false);
                }
                hasEnteredMenuSetting = false;
                tempTimer = null;  // will memory leak?
            };
        }

        private void btnSetting_Basic_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate to desired page
            PageSwitchControl<PageSetting>(ref pageSetting);
            ShowMenuSetting(false);
        }

        private void btnSetting_Scheduler_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PageCalendar>(ref pageCalendar);
            ShowMenuSetting(false);
        }

        private void ShowMenuSetting(bool show)
        {
            if (doesAnimationEnd)
            {
                doesAnimationEnd = false;

                DoubleAnimation da = new DoubleAnimation();
                da.From = show ? -70 : 0;
                da.To = show ? 0 : -70;

                da.Duration = TimeSpan.FromMilliseconds(70);
                da.Completed += (sender, args) =>
                {
                    doesAnimationEnd = true;
                    isMenuSettingShowing = show;
                };
                settingTranslateTransform.BeginAnimation(TranslateTransform.XProperty, da);
            }
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

        private void winMain_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TODO: global hotkey for player control
            if (MPLiteSetting.IsEditing) return;
            string cmd = MPLiteHotkeys.FindName(e, Keyboard.Modifiers);
            bool isHotkeyCmd = true;
#if DEBUG
            Console.WriteLine(DateTime.Now);
            Console.WriteLine(string.Format("{0}, {1}", e.SystemKey, e.Key));
#endif
            switch (cmd)
            {
                case "Play & Pause":
                    btnStartPlayback_Click(null, null);
                    break;
                case "Stop":
                    btnStop_Click(null, null);
                    break;
                case "Forward":
                    btnForward_Click(null, null);
                    break;
                case "Backward":
                    btnBackward_Click(null, null);
                    break;
                case "Increase Volume":
                    if (volumeBar.Value + 10 <= 1000)
                    {
                        volumeBar.Value += 10;
                        SetPlayerVolume(null, null);
                    }
                    break;
                case "Decrease Volume":
                    if (volumeBar.Value - 10 <= 1000)
                    {
                        volumeBar.Value -= 10;
                        SetPlayerVolume(null, null);
                    }
                    break;
                case "Fast Forward":
                    trackBar_PreviewMouseDown(null, null);
                    trackBar.Value = (trackBar.Value + trackBar.Maximum * 0.05 >= trackBar.Maximum) ? trackBar.Maximum : trackBar.Value + trackBar.Maximum * 0.05;
                    trackBar_PreviewMouseUp(null, null);
                    break;
                case "Reverse":
                    trackBar_PreviewMouseDown(null, null);
                    trackBar.Value = (trackBar.Value - trackBar.Maximum * 0.05 <= 0) ? 0 : trackBar.Value - trackBar.Maximum * 0.05;
                    trackBar_PreviewMouseUp(null, null);
                    break;
                default:
                    isHotkeyCmd = false;
                    break;
            }

            e.Handled = isHotkeyCmd;    // disable key beeping
        }

        private void winMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Dispose();
            Properties.Settings.Default.WindowHeight = this.ActualHeight;
            Properties.Settings.Default.WindowWidth = this.ActualWidth;
            Properties.Settings.Default.Save();

#if DEBUG
            writer.Close();
            ostrm.Close();
#endif
        }
        #endregion

        #region Music player control
        private void PlayTrackFromPageList(Guid playlistGUID, int selectedTrackIndex, PlaybackMode mode)
        {
            try
            {
                _musicPlayer.Play(GetTrackEvent(_musicPlayer, playlistGUID, selectedTrackIndex, mode, true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                        _musicPlayer.Play(GetTrackEvent(_musicPlayer, Guid.Empty, -1, (PlaybackMode)Properties.Settings.Default.PlaybackMode, true));
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

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            _musicPlayer.Stop();
            _musicPlayer.ClearQueue();
        }

        private void btnBackward_Click(object sender, RoutedEventArgs e)
        {
            if (!_musicPlayer.IsPlaying() && ! _musicPlayer.IsPaused())
                return;

            Guid listGUID = Properties.Settings.Default.TaskPlaylistGUID;
            PlaybackMode mode = (PlaybackMode)Properties.Settings.Default.PlaybackMode;

            try
            {
                _musicPlayer.Play(GetTrackEvent(_musicPlayer, listGUID, -1, mode, false));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            if (!_musicPlayer.IsPlaying() && !_musicPlayer.IsPaused())
                return;

            Guid listGUID = Properties.Settings.Default.TaskPlaylistGUID;
            PlaybackMode mode = (PlaybackMode)Properties.Settings.Default.PlaybackMode;

            try
            {
                _musicPlayer.Play(GetTrackEvent(_musicPlayer, listGUID, -1, mode, true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Subscriber
        public void ResetTimerAndTrackBar(TrackStatusEventArgs e)
        {
            timer.Stop();

            // Reset the posotion of thumb
            ResetTrackBar();
            // Reset icon of btn_StartPlayback
            btnStartPlayback.Content = FindResource("PlaybackCtrl_Play");
        }

        // Subscriber
        private void SetTimerAndTrackBar(TrackStatusEventArgs e)
        {
            trackBar.Maximum = _musicPlayer.GetSongLength();
            timer.Start();

            PlayerResumeHandler();
        }

        // Subscriber
        private void PlayerPauseHandler()
        {
            timer.Stop();
            btnStartPlayback.Content = FindResource("PlaybackCtrl_Play");
        }

        private void PlayerResumeHandler()
        {
            // Change icon of btn_StartPlayback to "Pause"
            btnStartPlayback.Content = FindResource("PlaybackCtrl_Pause");
        }

        // Subscriber
        private void StopOrPlayNextTrack()
        {
            // Play next track or replay the same track (according to user setting)
            Guid listGUID = Properties.Settings.Default.TaskPlaylistGUID;
            PlaybackMode mode = (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode;
            _musicPlayer.Play(GetTrackEvent(_musicPlayer, listGUID, -1, mode, true));
        }

        private TrackStatusEventArgs GetPlayingTrackStatus()
        {
            return new TrackStatusEventArgs(_musicPlayer.CurrentTrack, _musicPlayer.CurrentPlaylistGUID, _musicPlayer.CurrentTrackIndex);
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
                    //MessageBox.Show(ex.StackTrace);
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

        #region Volume control
        private void ShowOrHideVolumeBar(bool show)
        {
            volumeBar.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            btnBackward.Visibility = show ? Visibility.Hidden : Visibility.Visible;
            btnStop.Visibility = show ? Visibility.Hidden : Visibility.Visible;
            btnStartPlayback.Visibility = show ? Visibility.Hidden : Visibility.Visible;
            btnForward.Visibility = show ? Visibility.Hidden : Visibility.Visible;
        }

        private void btnVolumeControl_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowOrHideVolumeBar(!(volumeBar.Visibility == Visibility.Visible));
        }

        private void btnVolumeControl_MouseLeave(object sender, MouseEventArgs e)
        {
            DispatcherTimer tempTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            tempTimer.Start();
            tempTimer.Tick += (tSender, tArgs) =>
            {
                tempTimer.Stop();
                if (!hasEnteredVolumeBar)
                    ShowOrHideVolumeBar(hasEnteredVolumeBar);
            };
        }

        private void volumeBar_MouseEnter(object sender, MouseEventArgs e)
        {
            hasEnteredVolumeBar = true;
        }

        private void volumeBar_MouseLeave(object sender, MouseEventArgs e)
        {
            hasEnteredVolumeBar = false;
            ShowOrHideVolumeBar(false);
        }

        private void volumeBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (volTimer.IsEnabled)
                volTimer.Stop();

            SetPlayerVolume(null, null);
            SetVolumeIcon(MPLiteSetting.Volume, MPLiteSetting.IsMuted);
        }

        private void volumeBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!volTimer.IsEnabled)
                volTimer.Start();
        }

        private void SetPlayerVolume(object sender, EventArgs e)
        {
            if ((int)volumeBar.Value == Properties.Settings.Default.Volume)
                return;

            MPLiteSetting.Volume = (int)volumeBar.Value;
            _musicPlayer.SetVolume(MPLiteSetting.Volume);
            SetVolumeIcon(MPLiteSetting.Volume, MPLiteSetting.IsMuted);
        }

        private void SetVolumeIcon(double value, bool isMuted)
        {
            if (isMuted)
            {
                btnVolumeControl.Content = FindResource("Volume_muted");
            }
            else if (value > 850)
            {
                btnVolumeControl.Content = FindResource("Volume_100");
            }
            else if (value > 500)
            {
                btnVolumeControl.Content = FindResource("Volume_66");
            }
            else if (value > 150)
            {
                btnVolumeControl.Content = FindResource("Volume_33");
            }
            else
            {
                btnVolumeControl.Content = FindResource("Volume_0");
            }
        }

        private void btnVolumeControl_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MPLiteSetting.IsMuted = !MPLiteSetting.IsMuted;
            _musicPlayer.SetVolume(MPLiteSetting.Volume);
            SetVolumeIcon(MPLiteSetting.Volume, MPLiteSetting.IsMuted);
        }
        #endregion

        #region System tray control
        private void miClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void miShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void miHideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
        #endregion
    }
}
