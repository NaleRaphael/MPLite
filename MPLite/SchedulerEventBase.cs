using System;

namespace MPLite
{
    public delegate void SchedulerEventHandler(SchedulerEventArgs e);

    public enum PlaybackCommands
    {
        Stop = 0,
        Play = 1,
        Pause = 2
    }

    public class SchedulerEventArgs : EventArgs
    {
        public PlaybackCommands Command { get; set; }
        public string Playlist { get; set; }
        public int TrackIndex { get; set; }
        public MPLiteConstant.PlaybackMode Mode { get; set; }

        public SchedulerEventArgs()
        {
            Command = PlaybackCommands.Stop;
            Playlist = "";
            TrackIndex = -1;
            Mode = MPLiteConstant.PlaybackMode.None;
        }
    }
}
