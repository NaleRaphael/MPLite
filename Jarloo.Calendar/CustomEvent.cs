using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class CustomEvent : IEvent
    {
        public DateTime BeginningTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DispatcherTimer Timer { get; set; }
        public RecurringFrequencies RecurringFrequency { get; set; }
        public string EventText { get; set; }
        public int Rank { get; set; }
        public bool IsTriggered { get; set; }
        public bool Enabled { get; set; }
        public bool IgnoreTimeComponent { get; set; }
        public bool ReadOnlyEvent { get; set; }
        public bool ThisDayForwardOnly { get; set; }
        public CustomRecurringFrequenciesHandler CustomRecurringFunction { get; set; }

        public event TimerElapsedEventHandler EventStartsEvent;
        public event TimerElapsedEventHandler EventEndsEvent;

        public CustomEvent()
        {
            Duration = TimeSpan.Zero;
            Timer = null;
            RecurringFrequency = RecurringFrequencies.None;
            EventText = null;
            Rank = 1;
            IsTriggered = false;
            Enabled = true;
            IgnoreTimeComponent = false;
            ReadOnlyEvent = false;
            ThisDayForwardOnly = true;
        }

        public IEvent Clone()
        {
            return new CustomEvent
            {
                CustomRecurringFunction = CustomRecurringFunction,
                BeginningTime = BeginningTime,
                Duration = Duration,
                Timer = Timer,
                RecurringFrequency = RecurringFrequency,
                EventText = EventText,
                Rank = Rank,
                IsTriggered = IsTriggered,
                Enabled = Enabled,
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
                Timer.Stop();
                EventStartsEvent(this);

                // Set timer to time the duration of this event
                if (Duration != TimeSpan.Zero)
                {
                    Timer = new DispatcherTimer { Interval = Duration };
                    Timer.Tick += (s, a) =>
                    {
                        Timer.Stop();
                        EventEndsEvent(this);
                        Timer = null;
                    };
                    Timer.Start();
                }
            };
            Timer.Start();
        }
    }
}
