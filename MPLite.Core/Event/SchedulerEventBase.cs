using System;

namespace MPLite.Event
{
    using PlaybackMode = Core.PlaybackMode;

    public delegate void SchedulerEventHandler(SchedulerEventArgs e);

    public enum PlaybackCommands
    {
        Stop = 0,
        Play = 1,
        Pause = 2
    }

    public class SchedulerEventArgs : CustomEventArgs
    {
        public PlaybackCommands Command { get; set; }
        public Guid PlaylistGUID { get; set; }
        public int TrackIndex { get; set; }
        public PlaybackMode Mode { get; set; }

        public SchedulerEventArgs()
        {
            Command = PlaybackCommands.Stop;
            PlaylistGUID = Guid.Empty;
            TrackIndex = -1;
            Mode = PlaybackMode.None;
        }
    }

    public class SchedulerEventHandlerFactory : IEventHandlerFactory
    {
        public event SchedulerEventHandler SchedulerEvent;

        public SchedulerEventHandlerFactory(SchedulerEventHandler handler)
        {
            SchedulerEvent = handler;
        }

        public TimerElapsedEventHandler CreateStartingEventHandler(IEvent source)
        {
            TimerElapsedEventHandler handler;
            handler = (args) =>
            {
                SchedulerEvent(source.ActionStartsEventArgs as SchedulerEventArgs);
            };
            return handler;
        }

        public TimerElapsedEventHandler CreateEndingEventHandler(IEvent source)
        {
            TimerElapsedEventHandler handler;
            handler = (args) =>
            {
                SchedulerEvent(source.ActionEndsEventArgs as SchedulerEventArgs);
            };
            return handler;
        }
    }
}
