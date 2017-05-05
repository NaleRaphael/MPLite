using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using GongSolutions.Wpf.DragDrop;

namespace MPLite
{
    using TrackInfo = Core.TrackInfo;
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
    using MPLiteConstant = Core.MPLiteConstant;

    public class TracksViewModel : IDropTarget, INotifyPropertyChanged
    {
        private List<int> selectedIndices = new List<int>();
        private ObservableCollection<TrackInfo> soundtracks = new ObservableCollection<TrackInfo>();

        public event PropertyChangedEventHandler PropertyChanged;

        public string TracklistName { get; set; }
        public Guid TracklistGUID { get; set; }
        

        public ObservableCollection<TrackInfo> Soundtracks
        {
            get { return soundtracks; }
            set
            {
                soundtracks = value;
                NotifyPropertyChanged("Soundtracks");
            }
        }

        public List<int> SelectedIndices
        {
            get { return selectedIndices; }
            set
            {
                selectedIndices = value;
            }
        }

        public TracksViewModel(Playlist pl)
        {
            TracklistName = pl.ListName;
            TracklistGUID = pl.GUID;

        }

        public void UpdatePlaylistName(string listName)
        {
            TracklistName = listName;
        }

        public void UpdateSoundtracks(List<TrackInfo> source)
        {
            Soundtracks.FillContent(source);
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;

            IDataObject dataObject = dropInfo.Data as IDataObject;

            // look for drag&drop new files
            if (dataObject != null && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            DataObject dataObject = dropInfo.Data as DataObject;
            if (dataObject != null && dataObject.ContainsFileDropList())
            {
                IEnumerable<string> dropList = dataObject.GetFileDropList().Cast<string>();
                List<TrackInfo> tlist = new List<TrackInfo>();

                foreach(string filePath in dropList)
                {
                    if (!MPLiteConstant.validFileType.Contains(Path.GetExtension(filePath)))
                        continue;
                    try
                    {
                        TrackInfo track = TrackInfo.ParseSource(filePath);
                        tlist.Add(track);
                        Soundtracks.Add(track);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                if (tlist.Count != 0)
                    PlaylistCollection.AddPlaylist(tlist, TracklistName);
            }
            else
            {
                // Reorder tracks
                List<TrackInfo> tracks;

                if ((dropInfo.Data as List<TrackInfo>) == null)
                {
                    // Single file is dropped
                    tracks = new List<TrackInfo>();
                    tracks.Add(dropInfo.Data as TrackInfo);
                }
                else
                {
                    // Multiple files is dropped
                    tracks = dropInfo.Data as List<TrackInfo>;
                }
                    
                int insertIndex = (dropInfo.InsertIndex > selectedIndices[0]) ? 
                    dropInfo.InsertIndex - tracks.Count : dropInfo.InsertIndex;
                PlaylistCollection.ReorderTracks(TracklistGUID, SelectedIndices, insertIndex);
                UpdateSoundtracks(PlaylistCollection.GetPlaylist(TracklistGUID).Soundtracks);
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
