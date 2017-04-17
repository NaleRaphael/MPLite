using System;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public interface IEvent
    {
        Guid GUID { get; }
        DateTime BeginningTime { get; set; }
        TimeSpan Duration { get; set; }
        DispatcherTimer Timer { get; set; }
        
        RecurringFrequencies RecurringFrequency { get; set; }
        string EventText { get; set; }
        int Rank { get; set; }

        bool IsTriggered { get; }
        bool Enabled { get; set; }

        // True if the event details cannot be modified
        bool IgnoreTimeComponent { get; set; }

        // Delete event itself when it ends
        bool AutoDelete { get; set; }
        bool ReadOnlyEvent { get; set; }

        // If this is a recurring event, set this to true to make the event show up only from the day specified forward
        bool ThisDayForwardOnly { get; set; }

        Type EventArgsType { get; set; }

        CustomEventArgs EventStartsEventArgs { get; set; }
        event TimerElapsedEventHandler EventStartsEvent;
        CustomEventArgs EventEndsEventArgs { get; set; }
        event TimerElapsedEventHandler EventEndsEvent;
        event TimerElapsedEventHandler DestructMeEvent;

        IEvent Clone();

        void SetTimer();
        void DisposeTimer();
    }

    // Used to reset event for recurrsion
    public delegate void TimerElapsedEventHandler(IEvent evnt);

    public interface IEventHandlerFactory
    {
        TimerElapsedEventHandler CreateStartingEventHandler(IEvent source);
        TimerElapsedEventHandler CreateEndingEventHandler(IEvent source);
    }
}
