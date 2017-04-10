using System.Windows;
using System.Windows.Controls;

namespace MPLite
{
    public partial class PageCalendar : Page
    {
        // ref: https://social.msdn.microsoft.com/Forums/vstudio/en-US/1f99c3c1-aeea-45aa-a501-a5b54b262799/winformhost-control-does-not-shown-when-windows-allowtransparency-true?forum=wpf
        private ProxyWindow proxyWin = null;

        // TEST
        private int count = 0;

        #region Event
        public delegate void SchedulerIsTriggeredEventHandler(string selectedPlaylist = null, int selectedTrackIndex = -1,
            MPLiteConstant.PlaybackMode mode = MPLiteConstant.PlaybackMode.None);
        public static event SchedulerIsTriggeredEventHandler SchedulerIsTriggeredEvent;
        #endregion

        public PageCalendar()
        {
            InitializeComponent();
            InitializeCalendar();
        }

        private void InitializeCalendar()
        {
            // TODO: show events on calendar
            
        }

        #region Calendar control
        private void gridLeftContainer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToPrevMonth.Visibility = Visibility.Visible;
        }

        private void gridLeftContainer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToPrevMonth.Visibility = Visibility.Hidden;
        }

        private void gridRightContainer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToNextMonth.Visibility = Visibility.Visible;
        }

        private void gridRightContainer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToNextMonth.Visibility = Visibility.Hidden;
        }

        private void btnMoveToPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            calendar.MoveToPrevMonth();
        }

        private void btnMoveToNextMonth_Click(object sender, RoutedEventArgs e)
        {
            calendar.MoveToNextMonth();
        }

        private void btnMoveToCurrentDate_Click(object sender, RoutedEventArgs e)
        {
            calendar.MoveToCurrentMonth();
        }
        #endregion

        private void btn_AddEvent_Click(object sender, RoutedEventArgs e)
        {
            // Fire event to notify PagePlaylist play track.
            SchedulerIsTriggeredEvent("New Playlist", -1, MPLiteConstant.PlaybackMode.Default);
        }

        private void btnAddEventInCalendar_Click(object sender, RoutedEventArgs e)
        {
            //calendar.CurrentDate
            //calendar.Days[6].Notes = "TEST";
            calendar.Days[6].Events.Add("TEST" + count++);
        }

        
    }
}
