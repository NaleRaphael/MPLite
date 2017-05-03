using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MPLite.Event
{
    public class EventManager
    {
        private DispatcherTimer refreshTimer;
        private EventCollection ecdb;     // entry of database (recording all set events)

        public List<IEvent> ActivatedEvents { get; set; }    // events which are in the range of `NextRefreshingTime`
        public List<IEvent> EventDB { get; set; }
        public DateTime NextRefreshingTime { get; set; }

        public IEventHandlerFactory EventHandlerFactory { get; set; }
        public delegate void EventIsAddedEventHandler(IEvent evnt);
        public event EventIsAddedEventHandler EventIsAddedEvent;
        public delegate void EventIsDeletedEventHandler(IEvent evnt);
        public event EventIsDeletedEventHandler EventIsDeletedEvent;

        public TimeSpan RefreshingTimerIntervalUnit
        {
            get { return TimeSpan.FromDays(1); }
            /* TODO: set {...} */
        }

        public EventManager(IEventHandlerFactory handlerFacotry)
        {
            EventHandlerFactory = handlerFacotry;
            ActivatedEvents = new List<IEvent>();

            ecdb = new EventCollection();
            ecdb.DatabaseIsChanged += UpdateEventDB;

            ecdb.Initialize();      // TODO: Consider that should DatabaseIsChangedEvent should be fired when ecdb is just initialized?

            UpdateEventDB();
            InitRefreshTimer();     // Timer used to refresh ActivatedEvents list

            RefreshTasks();
        }

        // Before execute `AddEvent`, manager should check whether the event is in range (one day).
        public void AddEvent(IEvent evnt)
        {
            evnt.Initialize();      // Post-processing for those objects created by object initializer

            // Check
            try
            {
                CheckEventProperty(evnt);
            }
            catch
            {
                throw;
            }

            
            ecdb.AddEvent(evnt);    // Saved into database

            RefreshTasks();

            // Notify subscribber that event is added successfully
            EventIsAddedEvent(evnt);
        }

        public void DeleteEvent(Guid targetGUID)
        {
            // NOTE: delete event both in database and ActivatedList
            IEvent target = ecdb.GetEvent(targetGUID);
            if (target == null) return;

            // Delete target in both database and activated list
            ecdb.DeleteEvent(target.GUID);

            IEvent activatedTarget = ActivatedEvents.Find(x => x.GUID == targetGUID);
            if (activatedTarget != null)
            {
                activatedTarget.DisposeTimer();
                ActivatedEvents.Remove(activatedTarget);
            }

            EventIsDeletedEvent(target);
            target = null;
        }
        
        private void DeleteEvent(IEvent target)
        {
            if (target == null) return;

            target.DisposeTimer();

            ActivatedEvents.Remove(target);
            ecdb.DeleteEvent((CustomEvent)target);

            EventIsDeletedEvent(target);
            target = null;
        }

        public void UpdateEvent<T>(T target) where T : IEvent
        {
            if (target == null) return;

            target.DisposeTimer();
            ActivatedEvents.Remove(target);
            ecdb.UpdateEvent(target);

            UpdateEventDB();
            RefreshTasks();
        }

        private bool IsEventInRange(IEvent target)
        {
            if (target.BeginningTime > NextRefreshingTime)
                return false;
            if (target.BeginningTime <= DateTime.Now)
                return false;
            else return true;
        }

        private void CheckEventProperty(IEvent target)
        {
            if (target.AutoDelete && target.RecurringFrequency != RecurringFrequencies.None)
                throw new EventPropertyConflictException("Property `AutoDelete` should be \"false\" if `RecurringFreqeuncy` is not \"None\".");
            if (target.BeginningTime < DateTime.Now && target.RecurringFrequency == RecurringFrequencies.None)
                throw new EventPropertyConflictException("This is not a recurring event, so that its `BeginningTime` should be later than `DateTime.Now`.");
            if (target.ActionStartsEventArgs == null)
                throw new EmptyEventArgsException("EventArgs for beginning action is required, please check `ActionStartsEventArgs` of this event.");
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
            if (EventHandlerFactory == null)
                return;
                //throw new Exception("EventHandlerFactory should not be NULL");

            // TODO: before refresh tasks, check those activated timers and dispose them
            DisposeActivatedTimers();

            // Refresh tasks when this app is opened, or the day is changed. (e.g. 2017/04/10 -> 2017/04/11)
            // NOTE: check whether there will be some unfinished tasks being disposed?
            ActivatedEvents.Clear();

            foreach (IEvent ce in EventDB)
            {
                SetActivatedTask(ce);
            }
        }

        private void SetActivatedTask(IEvent ce)
        {
            // TODO: check that the beginning time of task is updated, otherwise the task won't be triggered
            ce.UpdateBeginningTime();
             
            if (IsEventInRange(ce))
            {
                if (ce.ActionEndsEventArgs != null)
                {
                    ce.ActionStartsEvent += EventHandlerFactory.CreateStartingEventHandler(ce);
                    if (ce.ActionEndsEventArgs != null)
                        ce.ActionEndsEvent += EventHandlerFactory.CreateEndingEventHandler(ce);
                }

                ActivatedEvents.Add(ce);

                if (ce.AutoDelete)
                    ce.SelfDestructEvent += DeleteEvent;

                ce.SetTimer();
            }
        }

        private void DisposeActivatedTimers()
        {
            foreach (IEvent ce in EventDB)
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
