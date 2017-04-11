using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace Jarloo.Calendar
{
    public class EventManager
    {
        // EventManger only have to handle events in one day.
        // Update event list when day is changed.
        public ICollection<IEvent> EventList;
        public ICollection<DispatcherTimer> TimerList;     // Once an event is added and it is in range (this day), create a timer for it.

        public EventManager()
        {
            EventList = new List<IEvent>();
            TimerList = new List<DispatcherTimer>();
        }

        // Before execute `AddEvent`, manager should check whether the event is in range (one day).
        public void AddEvent(IEvent evnt)
        {
            CheckEventIsInRange(evnt);  // TODO: not implemented yet
            evnt.SetTimer();

            EventList.Add(evnt);
            TimerList.Add(evnt.Timer);
        }

        // Refresh tasks when this app is opened, or the day is changed. (e.g. 2017/04/10 -> 2017/04/11)
        public void RefreshTasks()
        {
            // NOTE: check whether there will be some unfinished tasks being disposed?
        }

        private void CheckEventIsInRange(IEvent evnt)
        {

        }
    }
}
