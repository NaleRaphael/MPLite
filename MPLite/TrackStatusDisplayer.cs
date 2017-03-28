using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MPLite
{
    public class TrackStatusDispModule
    {
        public Label TrackProgress;
        public Label TrackName;
        private string trackDuration;

        public TrackStatusDispModule(Label lbl_trackProgress, Label lbl_trackName)
        {
            TrackProgress = lbl_trackProgress;
            TrackName = lbl_trackName;
            TrackProgress.Content = "";
            TrackName.Content = "";
        }

        public void SetTrackLength(PlayTrackEventArgs e)
        {
            trackDuration = e.Track.Duration;
        }

        public void SetTrackName(PlayTrackEventArgs e)
        {
            TrackName.Content = e.Track.TrackName;
        }

        public void ResetTrackName(PlayTrackEventArgs e)
        {
            TrackName.Content = "";
        }

        public void SetTrackProgress(int miliSecond)
        {
            TrackProgress.Content = string.Format("{0:00}", miliSecond / 60000) + ":" + string.Format("{0:00}", miliSecond / 1000 % 60) + "/" + trackDuration;
        }

        public void ResetTrackProgress(PlayTrackEventArgs e)
        {
            TrackProgress.Content = "";
        }
    }
}
