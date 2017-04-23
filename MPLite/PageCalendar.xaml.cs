using System;
using System.Windows;
using System.Windows.Controls;

namespace MPLite
{
    using CalendarViewingMode = Event.CalendarViewingMode;
    using PlaybackMode = Core.PlaybackMode;
    using PlaybackCommands = Event.PlaybackCommands;
    using RecurringFrequencies = Event.RecurringFrequencies;
    using CustomEvent = Event.CustomEvent;
    using SchedulerEventArgs = Event.SchedulerEventArgs;
    using SchedulerEventHandler = Event.SchedulerEventHandler;
    using SchedulerEventHandlerFactory = Event.SchedulerEventHandlerFactory;

    public partial class PageCalendar : Page
    {
        public static event SchedulerEventHandler SchedulerEvent;

        public PageCalendar()
        {
            InitializeComponent();
            InitializeCalendar();
        }

        private void InitializeCalendar()
        {
            // Assign custom HandlerEventFactory to Jarloo.Calendar to create handler
            calendar.OnInitialization(new SchedulerEventHandlerFactory(SchedulerEvent));
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
                Mode = PlaybackMode.Default,
                TrackIndex = -1
            };
            SchedulerEvent(se);
        }

        // TEST
        private void btnCalendarTester_Click(object sender, RoutedEventArgs e)
        {
            //calendar.Days[6].EventTexts.Add("TEST" + count++);
        }

        // TEST
        private void btnEventManagerTester_Click(object sender, RoutedEventArgs e)
        {
            CustomEvent evnt = new CustomEvent() {
                BeginningTime = DateTime.Now.AddSeconds(5),
                Duration = TimeSpan.FromSeconds(5),
                Enabled = true,
                EventText = "Test event",
                Rank = 1,
                ReadOnlyEvent = false,
                RecurringFrequency = RecurringFrequencies.EveryWeekday,
                ThisDayForwardOnly = true,
                IgnoreTimeComponent = false,
            };

            evnt.ActionStartsEventArgs = new SchedulerEventArgs
            {
                Playlist = "New Playlist",
                Command = PlaybackCommands.Play,
                Mode = PlaybackMode.Default,
                TrackIndex = -1
            };

            evnt.ActionEndsEventArgs = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Stop
            };

            try
            {
                calendar.EventManager.AddEvent(evnt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
            var result = MPLite.Event.Utils.ConvertToCustomWeekday(DateTime.Now.AddDays(3));
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
            bool result = MPLite.Event.Utils.IsDayInRange(source, source.AddDays(-5), CalendarViewingMode.Weekly);
            MessageBox.Show(result.ToString());
        }

        private void btnAddNewEvent_Click(object sender, RoutedEventArgs e)
        {
            CustomEvent evnt = new CustomEvent
            {
                BeginningTime = DateTime.Now.AddDays(-5).AddSeconds(10),
                Duration = TimeSpan.FromSeconds(5),
                Enabled = true,
                EventText = "Test event2",
                Rank = 1,
                ReadOnlyEvent = false,
                RecurringFrequency = RecurringFrequencies.EveryTuesday,
                ThisDayForwardOnly = true,
                IgnoreTimeComponent = true,
                AutoDelete = false,
            };
            
            evnt.ActionStartsEventArgs = new SchedulerEventArgs
            {
                Playlist = "New Playlist",
                Command = PlaybackCommands.Play,
                Mode = PlaybackMode.Default,
                TrackIndex = -1
            };
            
            evnt.ActionEndsEventArgs = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Stop
            };

            try
            {
                calendar.EventManager.AddEvent(evnt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
