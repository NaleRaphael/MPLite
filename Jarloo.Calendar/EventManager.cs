using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class EventManager
    {
        private DispatcherTimer refreshTimer;

        public ICollection<IEvent> EventList { get; set; }
        public ICollection<DispatcherTimer> TimerList { get; set; }     // Once an event is added and it is in range (this day), create a timer for it.
        public DateTime NextRefreshingTime { get; set; }

        public TimeSpan RefreshingTimerIntervalUnit
        {
            get { return TimeSpan.FromDays(1); }
            /* TODO: set {...} */
        }

        public EventManager()
        {
            EventList = new List<IEvent>();
            TimerList = new List<DispatcherTimer>();
            InitRefreshTimer();
        }

        // Before execute `AddEvent`, manager should check whether the event is in range (one day).
        public void AddEvent(IEvent evnt)
        {
            if (!CheckEventIsInRange(evnt))
                return;

            evnt.SetTimer();
            EventList.Add(evnt);
            TimerList.Add(evnt.Timer);
        }

        private bool CheckEventIsInRange(IEvent evnt)
        {
            TimeSpan diff = NextRefreshingTime - evnt.BeginningTime;
            if (diff <= TimeSpan.Zero || diff > RefreshingTimerIntervalUnit)
                return false;
            else
                return true;
        }

        #region refreshTimer control
        private void InitRefreshTimer()
        {
            if (refreshTimer != null)
                return;

            NextRefreshingTime = DateTime.Today.AddDays(1);
            refreshTimer = new DispatcherTimer { Interval = NextRefreshingTime - DateTime.Now };
            refreshTimer.Tick += (sender, args) =>
            {
                refreshTimer.Stop();

                // Update task lists
                RefreshTasks();

                // Reset refreshTimer
                refreshTimer = null;
                InitRefreshTimer();
            };
            refreshTimer.Start();
        }

        // Refresh tasks when this app is opened, or the day is changed. (e.g. 2017/04/10 -> 2017/04/11)
        public void RefreshTasks()
        {
            // NOTE: check whether there will be some unfinished tasks being disposed?
            return;
        }
        #endregion
    }
}
