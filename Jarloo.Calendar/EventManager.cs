﻿using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class EventManager
    {
        private DispatcherTimer refreshTimer;
        private EventCollection ecdb;     // entry of database (recording all set events)

        public List<CustomEvent> ActivatedEvent { get; set; }    // events which are in the range of `NextRefreshingTime`
        public DateTime NextRefreshingTime { get; set; }

        public TimeSpan RefreshingTimerIntervalUnit
        {
            get { return TimeSpan.FromDays(1); }
            /* TODO: set {...} */
        }

        public EventManager()
        {
            ecdb = new EventCollection();
            ecdb.Initialize();
            ActivatedEvent = new List<CustomEvent>();
            InitRefreshTimer();
        }

        // Before execute `AddEvent`, manager should check whether the event is in range (one day).
        public void AddEvent(CustomEvent evnt)
        {
            ecdb.AddEvent(evnt);      // Saved into database

            if (IsEventInRange(evnt))
            {
                ActivatedEvent.Add(evnt);
                if (evnt.AutoDelete)
                    evnt.DestructMeEvent += DestructEvent;
                evnt.SetTimer();
            }
        }

        private void DestructEvent(IEvent target)
        {
            ActivatedEvent.Remove((CustomEvent)target);
            ecdb.DeleteEvent((CustomEvent)target);
            target = null;
        }

        private bool IsEventInRange(CustomEvent target)
        {
            if (target.BeginningTime > NextRefreshingTime)
                return false;
            if (target.BeginningTime <= DateTime.Now)
                return false;
            else return true;
        }

        #region refreshTimer control
        private void InitRefreshTimer()
        {
            if (refreshTimer != null)
                return;

            NextRefreshingTime = DateTime.Today.AddDays(1);
            //NextRefreshingTime = DateTime.Now.AddSeconds(10);  // TEST
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

        public void RefreshTasks()
        {
            // Refresh tasks when this app is opened, or the day is changed. (e.g. 2017/04/10 -> 2017/04/11)
            // NOTE: check whether there will be some unfinished tasks being disposed?
            ActivatedEvent.Clear();

            foreach (CustomEvent ce in ecdb.EventList)
            {
                if (IsEventInRange(ce))
                {
                    ActivatedEvent.Add(ce);
                    ce.SetTimer();
                }
            }
        }
        #endregion
    }
}
