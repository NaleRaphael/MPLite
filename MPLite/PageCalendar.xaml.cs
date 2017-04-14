using System;
using System.Windows;
using System.Windows.Controls;

namespace MPLite
{
    public partial class PageCalendar : Page
    {
        // TEST
        private int count = 0;

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

        // TEST
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

        // TEST
        private void btnCalendarTester_Click(object sender, RoutedEventArgs e)
        {
            calendar.Days[6].EventTexts.Add("TEST" + count++);
        }

        // TEST
        private void btnEventManagerTester_Click(object sender, RoutedEventArgs e)
        {
            Jarloo.Calendar.CustomEvent evnt = new Jarloo.Calendar.CustomEvent {
                BeginningTime = DateTime.Now.AddSeconds(5),
                Duration = TimeSpan.FromSeconds(5),
                Enabled = true,
                EventText = "Test event",
                Rank = 1,
                ReadOnlyEvent = false,
                RecurringFrequency = Jarloo.Calendar.RecurringFrequencies.EveryWeekday,
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

            calendar.EventManager.AddEvent(evnt);
        }

        // TEST
        private void btnStopPlayerEventTester_Click(object sender, RoutedEventArgs e)
        {
            SchedulerEventArgs se = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Stop
            };
            SchedulerEvent(se);
        }

        // TEST
        private void btnPausePlayerEventTester_Click(object sender, RoutedEventArgs e)
        {
            SchedulerEventArgs se = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Pause
            };
            SchedulerEvent(se);
        }

        private void btnWeekdayConvert_Click(object sender, RoutedEventArgs e)
        {
            var result = Jarloo.Calendar.Utils.ConvertToCustomWeekday(DateTime.Now.AddDays(3));
            byte temp = (byte)result;
            MessageBox.Show(result.ToString());
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            /*
            var rf = Jarloo.Calendar.RecurringFrequencies.EveryWeekend;
            var result = Jarloo.Calendar.Utils.GetNextRecurringWeekday(Jarloo.Calendar.Weekday.Sunday, rf);
            MessageBox.Show(result.ToString());
            */

            /*
            DateTime nextWeekday = Jarloo.Calendar.Utils.DateTimeOfNextWeekday(DateTime.Now, DayOfWeek.Thursday);
            MessageBox.Show(nextWeekday.ToString());
            */

            /*
            // Find the range of week by given date
            DateTime wkb;
            DateTime wke;
            DateTime source = DateTime.Today.AddDays(-5);
            Jarloo.Calendar.Utils.FindRangeOfWeek(source, out wkb, out wke);
            MessageBox.Show(source.ToString() + "\n" + wkb.ToString() + ", " + wke.ToString());
            */

            DateTime source = DateTime.Today;
            bool result = Jarloo.Calendar.Utils.IsDayInRange(source, source.AddDays(-5), Jarloo.Calendar.CalenderViewingMode.Weekly);
            MessageBox.Show(result.ToString());
        }

        private void btnAddNewEvent_Click(object sender, RoutedEventArgs e)
        {
            Jarloo.Calendar.CustomEvent evnt = new Jarloo.Calendar.CustomEvent()
            {
                BeginningTime = DateTime.Now.AddSeconds(5),
                Duration = TimeSpan.FromSeconds(5),
                Enabled = true,
                EventText = "Test event2",
                Rank = 1,
                ReadOnlyEvent = false,
                RecurringFrequency = Jarloo.Calendar.RecurringFrequencies.Daily,
                ThisDayForwardOnly = false,
                IgnoreTimeComponent = true,
                AutoDelete = true,
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

            calendar.EventManager.AddEvent(evnt);
        }
    }
}
