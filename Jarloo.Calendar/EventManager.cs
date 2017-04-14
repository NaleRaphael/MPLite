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
        //public List<DispatcherTimer> ActivatedTimers { get; set; }
        public List<CustomEvent> EventDB { get; set; }
        public DateTime NextRefreshingTime { get; set; }

        public delegate void EventIsAddedEventHandler(IEvent evnt);
        public event EventIsAddedEventHandler EventIsAddedEvent;
        public delegate void EventIsDeletedEventHandler(IEvent evnt);
        public event EventIsDeletedEventHandler EventIsDeletedEvent;

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
            //ActivatedTimers = new List<DispatcherTimer>();
            InitRefreshTimer();

            // TODO: update --activated event list-- event database (those events are not expired yet)
            UpdateEventDB();
            RefreshTasks();     // Update activated tasks when program is started every time
        }

        // Before execute `AddEvent`, manager should check whether the event is in range (one day).
        public void AddEvent(CustomEvent evnt)
        {
            //evnt.SetGUID();
            ecdb.AddEvent(evnt);      // Saved into database
            UpdateEventDB();    // TODO: rewrite this. Add a event in EventCollection, fire it when there is a new event added. And then subscribe to it.

            // Fire an event to notify subscribber that event is added successfully
            // TODO: notify calender to update layout
            EventIsAddedEvent(evnt);

            if (IsEventInRange(evnt))
            {
                ActivatedEvent.Add(evnt);
                if (evnt.AutoDelete)
                    evnt.DestructMeEvent += DeleteEvent;

                evnt.SetTimer();

                // Add timer to watchlist, and it will be remove when the event is deleted.
                // If we don't monitor this timer, event will stll execute when it was deleted from database.
                //ActivatedTimers.Add(evnt.Timer);
            }
        }

        public void DeleteEvent(Guid targetGUID)
        {
            // NOTE: delete event both in database and ActivatedList
            IEvent target = ecdb.GetEvent(targetGUID);
            if (target == null) return;

            IEvent activatedTarget = ActivatedEvent.Find(x => x.GUID == targetGUID);
            activatedTarget.DisposeTimer();

            ActivatedEvent.Remove((CustomEvent)activatedTarget);
            //ActivatedTimers.Remove(target.Timer);
            ecdb.DeleteEvent(targetGUID);
            UpdateEventDB();

            EventIsDeletedEvent(target);
            target = null;
        }

        private void DeleteEvent(IEvent target)
        {
            if (target == null) return;

            target.DisposeTimer();

            ActivatedEvent.Remove((CustomEvent)target);
            //ActivatedTimers.Remove(target.Timer);
            ecdb.DeleteEvent((CustomEvent)target);
            UpdateEventDB();

            EventIsDeletedEvent(target);
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

        private void UpdateEventDB()
        {
            EventDB = ecdb.EventList;
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
                    //ActivatedTimers.Add(ce.Timer);
                }
            }
        }
        #endregion
    }

    public class NewlyAddedEventArgs : EventArgs
    {
        public IEvent NewEvent { get; set; }

        public NewlyAddedEventArgs(IEvent newEvent)
        {
            this.NewEvent = newEvent;
        }
    }
}
