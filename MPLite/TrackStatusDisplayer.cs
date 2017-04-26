using System.Windows.Controls;

namespace MPLite
{
    using PlayTrackEventArgs = Core.PlayTrackEventArgs;
    using TrackInfo = Core.TrackInfo;

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

        public void SetTrackLength(TrackStatusEventArgs e)
        {
            trackDuration = e.Track.Duration;
        }

        public void SetTrackName(TrackStatusEventArgs e)
        {
            TrackName.Content = e.Track.TrackName;
        }

        public void ResetTrackName(TrackStatusEventArgs e)
        {
            TrackName.Content = "";
        }

        public void SetTrackProgress(int miliSecond)
        {
            TrackProgress.Content = string.Format("{0:00}", miliSecond / 60000) + ":" + string.Format("{0:00}", miliSecond / 1000 % 60) + "/" + trackDuration;
        }

        public void ResetTrackProgress(TrackStatusEventArgs e)
        {
            TrackProgress.Content = "";
        }
    }
}
