﻿using System;
using System.Windows.Threading;

namespace MPLite.Event
{
    public class CustomEvent : IEvent
    {
        private EventHandler timerEventHandler;

        // NOTE: time component should be processed independently
        public Guid GUID { get; set; }
        public DateTime BeginningTime { get; set; }
        public DateTime OriginalBeginningTime { get; set; }     // CHECK: unnecessary?
        public TimeSpan Duration { get; set; }
        public DispatcherTimer Timer { get; set; }
        public RecurringFrequencies RecurringFrequency { get; set; }
        public string EventText { get; set; }
        public int Rank { get; set; }
        public bool IsTriggered { get; private set; }
        public bool Enabled { get; set; }
        public bool AutoDelete { get; set; }
        public bool IgnoreTimeComponent { get; set; }
        public bool ReadOnlyEvent { get; set; }
        public bool ThisDayForwardOnly { get; set; }

        public CustomEventArgs ActionStartsEventArgs { get; set; }
        public event TimerElapsedEventHandler ActionStartsEvent;
        public CustomEventArgs ActionEndsEventArgs { get; set; }
        public event TimerElapsedEventHandler ActionEndsEvent;
        public event TimerElapsedEventHandler SelfDestructEvent;  // workaround: try to implement `AutoDelete`

        public CustomEvent()
        {
            // TODO: Notify user if there is a conflict of RecurringFrequency and AutoDelete
            GUID = Guid.NewGuid();
            Duration = TimeSpan.Zero;
            Timer = null;
            RecurringFrequency = RecurringFrequencies.None;
            EventText = null;
            Rank = 1;
            IsTriggered = false;
            Enabled = true;
            AutoDelete = false;
            IgnoreTimeComponent = false;
            ReadOnlyEvent = false;
            ThisDayForwardOnly = true;
            OriginalBeginningTime = BeginningTime;
        }

        public virtual IEvent Clone()
        {
            return new CustomEvent
            {
                // TODO: can GUID clone too?
                BeginningTime = BeginningTime,
                OriginalBeginningTime = OriginalBeginningTime,
                Duration = Duration,
                Timer = Timer,
                RecurringFrequency = RecurringFrequency,
                EventText = EventText,
                Rank = Rank,
                IsTriggered = IsTriggered,
                Enabled = Enabled,
                AutoDelete = AutoDelete,
                IgnoreTimeComponent = IgnoreTimeComponent,
                ReadOnlyEvent = ReadOnlyEvent,
                ThisDayForwardOnly = ThisDayForwardOnly
            };
        }

        public virtual void Initialize()
        {
            // If this object is created by object initializer, this method should be called.
            OriginalBeginningTime = BeginningTime;
        }

        // Timer has to be set by EventManager, not start counting itself.
        public virtual void SetTimer()
        {
            IsTriggered = false;    // reset (for recurring event)

            Timer = new DispatcherTimer { Interval = BeginningTime - DateTime.Now };
            Console.WriteLine("Interval: " + (BeginningTime - DateTime.Now).ToString());
            timerEventHandler = (sender, args) =>
            {
                IsTriggered = true;
                if (Timer != null)
                {
                    Timer.Stop();
                    Timer.Tick -= timerEventHandler;    // check
                    timerEventHandler = null;   // check
                    Timer = null;
                }

                // Notify subscribers that this event starts
                ActionStartsEvent(this);

                // Timer for timing event duration
                // Update timer when event ends if `Duration` is not zero, otherwise update it directly.
                if (Duration != TimeSpan.Zero)
                {
                    DateTime prevBeginningTime = BeginningTime;
                    bool isUpToDate = UpdateBeginningTime();
                    TimeSpan buffer = TimeSpan.FromSeconds(1);
                    TimeSpan intv = BeginningTime - prevBeginningTime;

                    // Truncate timer interval if `Duration` is too long
                    Timer = new DispatcherTimer {
                        Interval = (Duration > intv && intv > buffer) ? intv - buffer : Duration
                    };

                    timerEventHandler = (s, a) =>
                    {
                        Timer.Stop();
                        Timer.Tick -= timerEventHandler;    // check
                        timerEventHandler = null;   // check
                        Timer = null;

                        // Notify subscribers that this event ends
                        ActionEndsEvent(this);

                        if (isUpToDate && BeginningTime > DateTime.Now)
                            SetTimer();

                        if (AutoDelete)
                            SelfDestructEvent(this);
                    };
                    Timer.Tick += timerEventHandler;
                    Timer.Start();
                }
                else if (this.UpdateBeginningTime() && BeginningTime > DateTime.Now)
                {
                    SetTimer();
                }
            };
            Timer.Tick += timerEventHandler;
            Timer.Start();
        }

        public virtual void DisposeTimer()
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer = null;
            }
        }

        /// <summary>
        /// Update `BeginningTime` accroding to `RecurringFrequency`.
        /// </summary>
        /// <returns>Boolean value indicating whether `BeginningTime` is up to date or not.</returns>
        public virtual bool UpdateBeginningTime()
        {
            if (RecurringFrequency == RecurringFrequencies.None)
                return false;
            if (BeginningTime > DateTime.Now)
                return true;    // BeginningTime is up-to-date.

            // assume that `dt` is today, check whether it has been expired or not. (if it is expired: starts from next day)
            DateTime beginningDateTime = BeginningTime.AddDays((DateTime.Today.Date - BeginningTime.Date).Days);
            // Compare time part of the day only
            DayOfWeek targetWeekday = (BeginningTime.TimeOfDay <= DateTime.Now.TimeOfDay) ? DateTime.Now.DayOfWeek + 1 : DateTime.Now.DayOfWeek;
            
            Weekday nextRecurringWeekday = Utils.GetNextRecurringWeekday(targetWeekday, RecurringFrequency);

            if (nextRecurringWeekday.ToSystemWeekday() == DateTime.Today.DayOfWeek)
                BeginningTime = DateTime.Today.Date.Add(BeginningTime.TimeOfDay);
            else
                BeginningTime = Utils.DateTimeOfNextWeekday(beginningDateTime, nextRecurringWeekday.ToSystemWeekday());

            return true;    
        }
    }

    public class CustomEventArgs : EventArgs
    {
        public CustomEventArgs() : base()
        {
        }
    }

    public class EventPropertyConflictException : Exception
    {
        public EventPropertyConflictException(string message) : base(message)
        {
        }
    }

    public class EmptyEventArgsException : Exception
    {
        public EmptyEventArgsException(string message) : base(message)
        {
        }
    }
}
