using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public interface IEvent
    {
<<<<<<< HEAD
        DateTime BeginningTime { get; set; }
=======
        Guid GUID { get; }
        DateTime BeginningTime { get; set; }
        DateTime OriginalBeginningTime { get; set; }
>>>>>>> rev8d0858e
        TimeSpan Duration { get; set; }
        DispatcherTimer Timer { get; set; }
        
        RecurringFrequencies RecurringFrequency { get; set; }
        string EventText { get; set; }
        int Rank { get; set; }

<<<<<<< HEAD
        bool IsTriggered { get; set; }
=======
        bool IsTriggered { get; }
>>>>>>> rev8d0858e
        bool Enabled { get; set; }

        // True if the event details cannot be modified
        bool IgnoreTimeComponent { get; set; }

        // Delete event itself when it ends
        bool AutoDelete { get; set; }
        bool ReadOnlyEvent { get; set; }

        // If this is a recurring event, set this to true to make the event show up only from the day specified forward
        bool ThisDayForwardOnly { get; set; }

<<<<<<< HEAD
        //CustomRecurringFrequenciesHandler CustomRecurringFunction { get; set; }

        event TimerElapsedEventHandler EventStartsEvent;
        event TimerElapsedEventHandler EventEndsEvent;
=======
        CustomEventArgs EventStartsEventArgs { get; set; }
        event TimerElapsedEventHandler EventStartsEvent;
        CustomEventArgs EventEndsEventArgs { get; set; }
        event TimerElapsedEventHandler EventEndsEvent;
        event TimerElapsedEventHandler DestructMeEvent;
>>>>>>> rev8d0858e

        IEvent Clone();

        void SetTimer();
<<<<<<< HEAD
    }

    // Used to reset event for recurrsion
    public delegate bool CustomRecurringFrequenciesHandler(IEvent evnt, DateTime day);
    public delegate void TimerElapsedEventHandler(IEvent evnt);
=======
        void DisposeTimer();
    }

    // Used to reset event for recurrsion
    public delegate void TimerElapsedEventHandler(IEvent evnt);

    public interface IEventHandlerFactory
    {
        TimerElapsedEventHandler CreateStartingEventHandler(IEvent source);
        TimerElapsedEventHandler CreateEndingEventHandler(IEvent source);
    }
>>>>>>> rev8d0858e
}
