using System;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;
using TagLib;


/// <summary>
/// ref: https://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx
/// ref: http://stackoverflow.com/questions/11210190/when-selecting-a-item-in-a-wpf-listview-update-other-controls-to-see-detail
/// </summary>
namespace MPLite.Core
{
    public class TrackInfo : INotifyPropertyChanged
    {
        [JsonIgnore]
        private string _statusSign;

        [JsonIgnore]
        private TrackStatus _trackStatus = TrackStatus.None;

        [JsonIgnore]
        public string StatusSign
        {
            get { return _statusSign; }
            set { _statusSign = value; NotifyPropertyChanged("StatusSign"); }
        }

        [JsonIgnore]
        public TrackStatus TrackStatus
        {
            get { return _trackStatus; }
            set
            {
                _trackStatus = value;
                StatusSign = MPLiteConstant.TrackStatusSign[(int)value];
                NotifyPropertyChanged("TrackStatus");
            }
        }

        public Guid GUID { get; set; }
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
            TagLib.File f = null;
            
            try
            {
                f = TagLib.File.Create(filePath, TagLib.ReadStyle.Average);
            }
            catch (CorruptFileException exCorruptFile)
            {
                // TODO: write id3 tag into this file
                //f = TagLib.File.Create(filePath, TagLib.ReadStyle.None);
                Exception ex = new Exception(String.Format("Cannot read mp3 header of: {0}", Path.GetFileName(filePath)), exCorruptFile);
                throw ex;
            }
            catch (Exception ex)
            {
                throw;
            }
            
            int min = ((int)f.Properties.Duration.TotalSeconds) / 60;
            int sec = ((int)f.Properties.Duration.TotalSeconds) % 60;
            string duration = string.Format("{0}:{1:00}", min, sec);

            TrackInfo track = new TrackInfo
            {
                GUID = Guid.NewGuid(),
                TrackName = (f.Tag.Title != null) ? f.Tag.Title : Path.GetFileNameWithoutExtension(filePath),
                TrackPath = filePath,
                Artist = (f.Tag.Performers.Length > 0) ? f.Tag.Performers[0] : "Unknown",
                Album = (f.Tag.Album != null) ? f.Tag.Album : "Unknown",
                Duration = duration
            };

            return track;
        }
    }
}
