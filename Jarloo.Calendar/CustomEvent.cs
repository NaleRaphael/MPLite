using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class CustomEvent : IEvent
    {
        // NOTE: time component should be processed independently
        public Guid GUID { get; set; }
        public DateTime BeginningTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DispatcherTimer Timer { get; set; }
        public RecurringFrequencies RecurringFrequency { get; set; }
        public string EventText { get; set; }
        public int Rank { get; set; }
        public bool IsTriggered { get; set; }
        public bool Enabled { get; set; }
        public bool AutoDelete { get; set; }
        public bool IgnoreTimeComponent { get; set; }
        public bool ReadOnlyEvent { get; set; }
        public bool ThisDayForwardOnly { get; set; }
        //public CustomRecurringFrequenciesHandler CustomRecurringFunction { get; set; }

        public event TimerElapsedEventHandler EventStartsEvent;
        public event TimerElapsedEventHandler EventEndsEvent;
        public event TimerElapsedEventHandler DestructMeEvent;  // workaround: try to implement `AutoDelete`

        public CustomEvent()
        {
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
        }

        public IEvent Clone()
        {
            return new CustomEvent
            {
                //CustomRecurringFunction = CustomRecurringFunction,
                // TODO: can GUID clone too?
                BeginningTime = BeginningTime,
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
            Timer = new DispatcherTimer { Interval = BeginningTime - DateTime.Now };
            Timer.Tick += (sender, args) =>
            {
                IsTriggered = true;
                Timer.Stop();
                Timer = null;

                // Notify subscribers that this event starts
                EventStartsEvent(this);

                // Update the next beginningTime of this event if it is a recurring event
                UpdateBeginningTime();

                // TODO: refresh `IsTriggered` property?

                // Set timer to time the duration of this event
                if (Duration != TimeSpan.Zero)
                {
                    Timer = new DispatcherTimer { Interval = Duration };
                    Timer.Tick += (s, a) =>
                    {
                        Timer.Stop();

                        // Notify subscribers that this event ends
                        EventEndsEvent(this);
                        Timer = null;

                        if (AutoDelete)
                            DestructMeEvent(this);
                    };
                    Timer.Start();
                }
            };
            Timer.Start();
        }

        private void UpdateBeginningTime()
        {
            if (RecurringFrequency == RecurringFrequencies.None)
                return;

            Weekday nextRecurringWeekday = Utils.GetNextRecurringWeekday(BeginningTime.DayOfWeek.ToCustomWeekday(), RecurringFrequency);
            BeginningTime = Utils.DateTimeOfNextWeekday(BeginningTime, nextRecurringWeekday.ToSystemWeekday());
        }
    }
}
