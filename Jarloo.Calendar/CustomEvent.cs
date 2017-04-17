using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class CustomEvent : IEvent
    {
        private EventHandler timerEventHandler;

        // NOTE: time component should be processed independently
        public Guid GUID { get; set; }
        public DateTime BeginningTime { get; set; }
        public DateTime OriginalBeginningTime { get; set; }
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

        public Type EventArgsType { get; set; }

        public CustomEventArgs EventStartsEventArgs { get; set; }
        public event TimerElapsedEventHandler EventStartsEvent;
        public CustomEventArgs EventEndsEventArgs { get; set; }
        public event TimerElapsedEventHandler EventEndsEvent;
        public CustomEventArgs DestructMeEventArgs { get; set; }
        public event TimerElapsedEventHandler DestructMeEvent;  // workaround: try to implement `AutoDelete`

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
            AutoDelete = true;
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
                EventStartsEvent(this);

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
                        EventEndsEvent(this);

                        if (AutoDelete)
                            DestructMeEvent(this);
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

        //private void UpdateBeginningTime()
        public void UpdateBeginningTime()
        {
            if (RecurringFrequency == RecurringFrequencies.None)
                return;
            if (BeginningTime > DateTime.Now)
                return;

            // assume that `dt` is today, check whether it has been expired or not. (if it is expired: starts from next day)
            DateTime beginningDateTime = BeginningTime.AddDays((DateTime.Today.Date - BeginningTime.Date).Days);
            DayOfWeek targetWeekday = (BeginningTime.TimeOfDay <= DateTime.Now.TimeOfDay) ? DateTime.Now.DayOfWeek + 1 : DateTime.Now.DayOfWeek;

            Weekday nextRecurringWeekday = Utils.GetNextRecurringWeekday(targetWeekday, RecurringFrequency);
            BeginningTime = Utils.DateTimeOfNextWeekday(beginningDateTime, nextRecurringWeekday.ToSystemWeekday());
        }

        private void CheckPropertyConflict()
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
}
