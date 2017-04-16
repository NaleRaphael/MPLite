using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class EventManager
    {
        private DispatcherTimer refreshTimer;
        private EventCollection ecdb;     // entry of database (recording all set events)

        public List<CustomEvent> ActivatedEvents { get; set; }    // events which are in the range of `NextRefreshingTime`
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
            ActivatedEvents = new List<CustomEvent>();

            ecdb = new EventCollection();
            ecdb.DatabaseIsChanged += UpdateEventDB;

            ecdb.Initialize();      // TODO: Consider that should DatabaseIsChangedEvent should be fired when ecdb is just initialized?

            UpdateEventDB();
            InitRefreshTimer();     // Timer used to refresh ActivatedEvents list

            /*
            ecdb.Initialize();
            ActivatedEvent = new List<CustomEvent>();
            //ActivatedTimers = new List<DispatcherTimer>();
            InitRefreshTimer();

            // TODO: update --activated event list-- event database (those events are not expired yet)
            UpdateEventDB();
            RefreshTasks();     // Update activated tasks when program is started every time
            */
        }

        // Before execute `AddEvent`, manager should check whether the event is in range (one day).
        public void AddEvent(CustomEvent evnt)
        {
            //evnt.SetGUID();
            ecdb.AddEvent(evnt);      // Saved into database
            //UpdateEventDB();    // TODO: rewrite this. Add a event in EventCollection, fire it when there is a new event added. And then subscribe to it.

            // TODO: RefreshTasks should be provided for a single-added task
            RefreshTasks();

            // Fire an event to notify subscribber that event is added successfully
            // TODO: notify calender to update layout
            EventIsAddedEvent(evnt);


            /*
            if (IsEventInRange(evnt))
            {
                ActivatedEvents.Add(evnt);
                if (evnt.AutoDelete)
                    evnt.DestructMeEvent += DeleteEvent;

                evnt.SetTimer();

                // Add timer to watchlist, and it will be remove when the event is deleted.
                // If we don't monitor this timer, event will stll execute when it was deleted from database.
                //ActivatedTimers.Add(evnt.Timer);
            }
            */
        }

        public void DeleteEvent(Guid targetGUID)
        {
            // NOTE: delete event both in database and ActivatedList
            IEvent target = ecdb.GetEvent(targetGUID);
            if (target == null) return;

            // Delete target in both database and activated list
            ecdb.DeleteEvent(target.GUID);
            // UpdateEventDB();

            IEvent activatedTarget = ActivatedEvents.Find(x => x.GUID == targetGUID);
            if (activatedTarget != null)
            {
                activatedTarget.DisposeTimer();
                ActivatedEvents.Remove((CustomEvent)activatedTarget);
            }

            /*
            activatedTarget.DisposeTimer();

            ActivatedEvent.Remove((CustomEvent)activatedTarget);
            //ActivatedTimers.Remove(target.Timer);
            ecdb.DeleteEvent(targetGUID);
            UpdateEventDB();
            */
            EventIsDeletedEvent(target);
            target = null;
        }

        private void DeleteEvent(IEvent target)
        {
            if (target == null) return;

            target.DisposeTimer();

            ActivatedEvents.Remove((CustomEvent)target);
            ecdb.DeleteEvent((CustomEvent)target);
            //UpdateEventDB();

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
            // TODO: should previous EventDB be set as null for GC?
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

        // TODO: write an overloaded function of RefreshTasks for single-added task
        //       otherwise, too much works have to be done when there is only one task is added
        public void RefreshTasks()
        {
            // TODO: before refresh tasks, check those activated timers and dispose them
            DisposeActivatedTimers();

            // Refresh tasks when this app is opened, or the day is changed. (e.g. 2017/04/10 -> 2017/04/11)
            // NOTE: check whether there will be some unfinished tasks being disposed?
            ActivatedEvents.Clear();

            foreach (CustomEvent ce in EventDB)
            {
                SetActivatedTask(ce);
                /*
                if (IsEventInRange(ce))
                {
                    ActivatedEvents.Add(ce);

                    if (ce.AutoDelete)
                        ce.DestructMeEvent += DeleteEvent;

                    ce.SetTimer();
                }
                */
            }
        }

        private void SetActivatedTask(CustomEvent ce)
        {
            if (IsEventInRange(ce))
            {
                ActivatedEvents.Add(ce);

                if (ce.AutoDelete)
                    ce.DestructMeEvent += DeleteEvent;

                ce.SetTimer();
            }

        }

        private void DisposeActivatedTimers()
        {
            foreach (CustomEvent ce in EventDB)
            {
                ce.DisposeTimer();
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
