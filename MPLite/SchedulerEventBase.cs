using Jarloo.Calendar;

namespace MPLite
{
    using PlaybackMode = Core.PlaybackMode;
    public delegate void SchedulerEventHandler(SchedulerEventArgs e);

    public enum PlaybackCommands
    {
        Stop = 0,
        Play = 1,
        Pause = 2
    }

    public class SchedulerEventArgs : Jarloo.Calendar.CustomEventArgs
    {
        public PlaybackCommands Command { get; set; }
        public string Playlist { get; set; }
        public int TrackIndex { get; set; }
        public PlaybackMode Mode { get; set; }

        public SchedulerEventArgs()
        {
            Command = PlaybackCommands.Stop;
            Playlist = "";
            TrackIndex = -1;
            Mode = PlaybackMode.None;
        }
    }

    public class SchedulerEventHandlerFactory : Jarloo.Calendar.IEventHandlerFactory
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
