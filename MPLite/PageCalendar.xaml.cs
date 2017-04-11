using System;
using System.Windows;
using System.Windows.Controls;

namespace MPLite
{
    public partial class PageCalendar : Page
    {
        // TEST
        private int count = 0;

        public Jarloo.Calendar.EventManager MPLiteEventManager;

        #region Event
        public static event SchedulerEventHandler SchedulerEvent;
        #endregion

        public PageCalendar()
        {
            InitializeComponent();
            InitializeCalendar();
        }

        private void InitializeCalendar()
        {
            // TODO: show events on calendar
            MPLiteEventManager = new Jarloo.Calendar.EventManager();
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

        private void btnMusicPlayerEventTester_Click(object sender, RoutedEventArgs e)
        {
            // Fire event to notify PagePlaylist play track.
            SchedulerEventArgs se = new SchedulerEventArgs
            {
                Playlist = "New Playlist",
                Command = PlaybackCommands.Play,
                Mode = MPLiteConstant.PlaybackMode.Default,
                TrackIndex = -1
            };
            SchedulerEvent(se);
        }

        private void btnCalendarTester_Click(object sender, RoutedEventArgs e)
        {
            calendar.Days[6].Events.Add("TEST" + count++);
        }

        private void btnEventManagerTester_Click(object sender, RoutedEventArgs e)
        {
            Jarloo.Calendar.CustomEvent evnt = new Jarloo.Calendar.CustomEvent {
                BeginningTime = DateTime.Now.AddSeconds(5),
                Duration = TimeSpan.FromSeconds(5),
                Enabled = true,
                EventText = "Test event",
                Rank = 1,
                ReadOnlyEvent = false,
                RecurringFrequency = Jarloo.Calendar.RecurringFrequencies.None,
                ThisDayForwardOnly = true,
                IgnoreTimeComponent = true,
            };

            evnt.EventStartsEvent += (args) =>
            {
                SchedulerEventArgs se = new SchedulerEventArgs
                {
                    Playlist = "New Playlist",
                    Command = PlaybackCommands.Play,
                    Mode = MPLiteConstant.PlaybackMode.Default,
                    TrackIndex = -1
                };
                SchedulerEvent(se);
            };

            evnt.EventEndsEvent += (args) =>
            {
                MessageBox.Show("Event ends.");
            };

            MPLiteEventManager.AddEvent(evnt);
        }

        private void btnStopPlayerEventTester_Click(object sender, RoutedEventArgs e)
        {
            SchedulerEventArgs se = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Stop
            };
            SchedulerEvent(se);
        }
    }
}
