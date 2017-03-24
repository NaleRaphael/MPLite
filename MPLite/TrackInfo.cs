using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;
using TagLib;


/// <summary>
/// ref: https://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx
/// ref: http://stackoverflow.com/questions/11210190/when-selecting-a-item-in-a-wpf-listview-update-other-controls-to-see-detail
/// </summary>
namespace MPLite
{
    public class TrackInfo : INotifyPropertyChanged
    {
        [JsonIgnore]
        private string _playingSign;
        [JsonIgnore]
        public string PlayingSign {
            get { return _playingSign; }
            set { _playingSign = value; NotifyPropertyChanged("PlayingSign");  }
        }

        public string TrackName { get; set; }
        public string TrackPath { get; set; }
        public string Duration { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static TrackInfo ParseSource(string filePath)
        {
            TagLib.File f = TagLib.File.Create(filePath, TagLib.ReadStyle.Average);

            int min = ((int)f.Properties.Duration.TotalSeconds) / 60;
            int sec = ((int)f.Properties.Duration.TotalSeconds) % 60;
            string duration = string.Format("{0}:{1}", min, sec);

            TrackInfo track = new TrackInfo {
                TrackName = (f.Tag.Title != null) ? f.Tag.Title: Path.GetFileNameWithoutExtension(filePath),
                TrackPath = filePath,
                Artist = (f.Tag.Performers.Length > 0) ? f.Tag.Performers[0] : "Unknown",
                Album = (f.Tag.Album != null) ? f.Tag.Album : "Unknown",
                Duration = duration
            };

            return track;
        }
    }
}
