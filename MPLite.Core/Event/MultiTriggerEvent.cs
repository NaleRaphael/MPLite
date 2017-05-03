using System;
using System.Collections.Generic;

namespace MPLite.Event
{
    public class MultiTriggerEvent : CustomEvent
    {
        public Queue<DateTime> BeginningTimeQueue { get; private set; }

        public MultiTriggerEvent(Queue<DateTime> beginningTimeQueue) : base()
        {
            BeginningTimeQueue = beginningTimeQueue;
            OriginalBeginningTime = beginningTimeQueue.Peek();
        }

        public override void Initialize()
        {
            BeginningTime = BeginningTimeQueue.Peek();
            OriginalBeginningTime = BeginningTime;
        }

        public override void CloneTo(IEvent target)
        {
            base.CloneTo(target);
            (target as MultiTriggerEvent).BeginningTimeQueue = this.BeginningTimeQueue;
        }

        public override bool UpdateBeginningTime()
        {
            if (BeginningTimeQueue.Count == 0)
                return false;

            while (BeginningTime.TimeOfDay <= DateTime.Now.TimeOfDay && BeginningTimeQueue.Count > 0)
            {
                BeginningTime = BeginningTimeQueue.Dequeue();
            }

            return base.UpdateBeginningTime();
        }
    }
}
