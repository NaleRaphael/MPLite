﻿using System;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace MPLite.Event
{
    public interface IEvent
    {
        Guid GUID { get; }
        DateTime BeginningTime { get; set; }
        DateTime OriginalBeginningTime { get; set; }
        TimeSpan Duration { get; set; }

        [JsonIgnore]
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

        CustomEventArgs ActionStartsEventArgs { get; set; }
        event TimerElapsedEventHandler ActionStartsEvent;
        CustomEventArgs ActionEndsEventArgs { get; set; }
        event TimerElapsedEventHandler ActionEndsEvent;
        event TimerElapsedEventHandler SelfDestructEvent;

        void CloneTo(IEvent target);

        // Post-processing of object creation if it is created by onject initializer
        void Initialize();

        void SetTimer();
        void DisposeTimer();
        bool UpdateBeginningTime();
    }

    // Used to reset event for recurrsion
    public delegate void TimerElapsedEventHandler(IEvent evnt);

    public interface IEventHandlerFactory
    {
        TimerElapsedEventHandler CreateStartingEventHandler(IEvent source);
        TimerElapsedEventHandler CreateEndingEventHandler(IEvent source);
    }
}
