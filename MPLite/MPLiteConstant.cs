﻿namespace MPLite
{
    public class MPLiteConstant
    {
        public static string[] validFileType = Properties.Settings.Default.ValidFileType.Split('|');

        public enum PlaybackMode
        {
            /// <summary>
            /// No specific playback mode.
            /// </summary>
            None = -1,
            /// <summary>
            /// Play entire playlist once.
            /// </summary>
            Default = 0,
            /// <summary>
            /// Play single track repeatedly.
            /// </summary>
            RepeatTrack,
            /// <summary>
            /// Play single list repeatedly.
            /// </summary>
            RepeatList,
            /// <summary>
            /// Randomly play track inside a list. After all track is played, player stops.
            /// </summary>
            ShuffleOnce,
            /// <summary>
            /// Randomly play track inside a list.
            /// </summary>
            Shuffle,
            /// <summary>
            /// Once a track ends, player stops.
            /// </summary>
            PlaySingle
        }

        public enum TrackStatus
        {
            None = 0,
            Playing = 1,
            Paused,
            Stopped,
            IncorrectPath
        }

        public static string[] TrackStatusSign = { "", ">", ">", "", "(!)"};
    }
}
