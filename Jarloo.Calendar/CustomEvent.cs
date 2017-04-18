﻿using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
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

        public IEvent Clone()
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

        // Timer has to be set by EventManager, not start counting itself.
        public void SetTimer()
        {
            IsTriggered = false;    // reset (for recurring event)

            Timer = new DispatcherTimer { Interval = BeginningTime - DateTime.Now };
            timerEventHandler = (sender, args) =>
            {
                IsTriggered = true;
                Timer.Stop();
                Timer.Tick -= timerEventHandler;    // check
                timerEventHandler = null;   // check
                Timer = null;

                // Notify subscribers that this event starts
                ActionStartsEvent(this);

                // Update the next beginningTime of this event if it is a recurring event
                UpdateBeginningTime();

                // Timer for timing event duration
                if (Duration != TimeSpan.Zero)
                {
                    Timer = new DispatcherTimer { Interval = Duration };

                    timerEventHandler = (s, a) =>
                    {
                        Timer.Stop();
                        Timer.Tick -= timerEventHandler;    // check
                        timerEventHandler = null;   // check
                        Timer = null;

                        // Notify subscribers that this event ends
                        ActionEndsEvent(this);

                        if (AutoDelete)
                            SelfDestructEvent(this);
                    };
                    Timer.Tick += timerEventHandler;
                    timerEventHandler = null;
                    Timer.Start();
                }
            };
            Timer.Tick += timerEventHandler;
            Timer.Start();
        }

        public void DisposeTimer()
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer = null;
            }
        }

        public void UpdateBeginningTime()
        {
            if (RecurringFrequency == RecurringFrequencies.None)
                return;
            if (BeginningTime > DateTime.Now)
                return;

            // assume that `dt` is today, check whether it has been expired or not. (if it is expired: starts from next day)
            DateTime beginningDateTime = BeginningTime.AddDays((DateTime.Today.Date - BeginningTime.Date).Days);
            // Compare time part of the day only
            DayOfWeek targetWeekday = (BeginningTime.TimeOfDay <= DateTime.Now.TimeOfDay) ? DateTime.Now.DayOfWeek + 1 : DateTime.Now.DayOfWeek;
            
            Weekday nextRecurringWeekday = Utils.GetNextRecurringWeekday(targetWeekday, RecurringFrequency);

            if (nextRecurringWeekday.ToSystemWeekday() == DateTime.Today.DayOfWeek)
                BeginningTime = DateTime.Today.Date.Add(BeginningTime.TimeOfDay);
            else
                BeginningTime = Utils.DateTimeOfNextWeekday(beginningDateTime, nextRecurringWeekday.ToSystemWeekday());
        }

        // TODO: this method cannot be called if this object is created by object initializer, try another method
        private void CheckEventPropertyConflict()
        {
            if (AutoDelete && RecurringFrequency != RecurringFrequencies.None)
                throw new Exception("Property `AutoDelete` should be false if `RecurringFreqeuncy` is not \"None\".");
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
