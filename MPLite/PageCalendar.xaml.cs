using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (proxyWin != null)
            {
                proxyWin.Close();
                proxyWin = null;
            }
        }

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
