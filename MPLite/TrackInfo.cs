using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;


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
    }
}
