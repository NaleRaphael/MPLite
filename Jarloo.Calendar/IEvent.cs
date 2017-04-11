using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    [Flags]
    public enum RecurringFrequencies
    {
        None = 0x00,
        EverySunday = 0x01,
        EveryMonday = 0x02,
        EveryTuesday = 0x04,
        EveryWendsday = 0x08,
        EveryThursday = 0x10,
        EveryFriday = 0x20,
        EverySaturday = 0x40,
        EveryWeekday = EveryMonday | EveryTuesday | EveryWendsday | EveryThursday | EveryFriday,
        EveryWeekend = EverySunday | EverySaturday,
        Daily = EveryWeekday | EveryWeekend,
        Monthly = 0x80,
        Yearly = 0x100,
        Custom = 0x200
    }

    public interface IEvent
    {
        DateTime BeginningTime { get; set; }
        TimeSpan Duration { get; set; }
        DispatcherTimer Timer { get; set; }
        bool IsTriggered { get; set; }
        bool Enabled { get; set; }
        RecurringFrequencies RecurringFrequency { get; set; }
        string EventText { get; set; }
        int Rank { get; set; }

        // True if the event details cannot be modified
        bool IgnoreTimeComponent { get; set; }

        bool ReadOnlyEvent { get; set; }

        // If this is a recurring event, set this to true to make the event show up only from the day specified forward
        bool ThisDayForwardOnly { get; set; }

        CustomRecurringFrequenciesHandler CustomRecurringFunction { get; set; }

        event TimerElapsedEventHandler EventStartsEvent;
        event TimerElapsedEventHandler EventEndsEvent;

        IEvent Clone();

        // Timer has to be set by EventManager, not start counting itself.
        void SetTimer();
    }

    // Used to reset event for recurrsion
    public delegate bool CustomRecurringFrequenciesHandler(IEvent evnt, DateTime day);
    public delegate void TimerElapsedEventHandler(IEvent evnt);
}
