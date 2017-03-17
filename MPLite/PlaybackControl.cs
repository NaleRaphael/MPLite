using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
{
    class PlaybackControl
    {
        public int PlayingTrackIndex { get; set; }
        public bool IsPlaying { get; set; }

        public PlaybackControl()
        {
            PlayingTrackIndex = 0;
            IsPlaying = false;
        }
    }
}
