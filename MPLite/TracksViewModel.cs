using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public delegate void PlaylistIsUpdatedEventHandler(UpdatePlaylistEventArgs e);
        public event PlaylistIsUpdatedEventHandler PlaylistIsUpdatedEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        public string TracklistName { get; private set; }
        public Guid TracklistGUID { get; private set; }
        public TrackInfo PlayingTrack { get; private set; }

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

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Constructor
        public TracksViewModel(Playlist pl)
        {
            TracklistName = (pl == null) ? null : pl.ListName;
            TracklistGUID = (pl == null) ? Guid.Empty : pl.GUID;
        }
        #endregion

        public void UpdatePlaylistInfo(Playlist pl)
        {
            TracklistGUID = pl.GUID;
            TracklistName = pl.ListName;
        }

        public void UpdatePlaylistInfo(Guid listGUID, string listName)
        {
            TracklistGUID = listGUID;
            TracklistName = listName;
        }
        
        public void UpdateSoundtracks(List<TrackInfo> source)
        {
            Soundtracks.FillContent(source);
        }

        public void UpdateSoundtracks(Playlist pl)
        {
            Soundtracks.FillContent(pl.Soundtracks);
        }

        public void UpdateTrackStatus(TrackStatusEventArgs e)
        {
            if (e == null || e.Index == -1)
                return;

            PlayingTrack = e.Track;
            TrackInfo track = soundtracks.FirstOrDefault(x => x.GUID == e.Track.GUID);
            if (track != null)
                track.TrackStatus = e.Track.TrackStatus;
            else return;
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
                {
                    Playlist pl = PlaylistCollection.UpdatePlaylist(tlist, TracklistName);
                    UpdatePlaylistEventArgs e = new UpdatePlaylistEventArgs(pl, PlayingTrack);
                    PlaylistIsUpdatedEvent?.Invoke(e);
                }
            }
            else
            {
                // workaround: There is a bug that user can drag item from the gap between two items.
                //    It triggers DragEvent without SelectionChangeEvent. So that `selectedIndices` will be empty.
                if (selectedIndices.Count == 0)
                    return;

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

                Playlist pl = PlaylistCollection.ReorderTracks(TracklistGUID, SelectedIndices, dropInfo.InsertIndex);
                if (pl == null) return;
                UpdateSoundtracks(pl.Soundtracks);

                UpdatePlaylistEventArgs e = new UpdatePlaylistEventArgs(pl, PlayingTrack);
                PlaylistIsUpdatedEvent?.Invoke(e);

                // reset PlayStatus
                if (PlayingTrack != null)
                {
                    TrackInfo track = soundtracks.FirstOrDefault(x => x.GUID == PlayingTrack.GUID);
                    if (track != null) track.TrackStatus = PlayingTrack.TrackStatus;
                }
            }
        }

        public void RemoveTracks()
        {
            PlaylistCollection.DeleteTracksByIndices(selectedIndices.ToArray(), TracklistGUID);
            for (int i = selectedIndices.Count - 1; i >= 0; i--)
            {
                soundtracks.RemoveAt(selectedIndices[i]);
            }

            Playlist pl = PlaylistCollection.GetPlaylist(TracklistGUID);
            UpdatePlaylistEventArgs e = new UpdatePlaylistEventArgs(pl, PlayingTrack);
            PlaylistIsUpdatedEvent?.Invoke(e);
        }
    }

    public class UpdatePlaylistEventArgs : EventArgs
    {
        public Playlist UpdatedPlaylist { get; set; }
        public TrackInfo PlayingTrack { get; set; }
        public int IndexOfPlayingTrack { get; set; }

        public UpdatePlaylistEventArgs(Playlist updatedPlaylist, TrackInfo playingTrack)
        {
            UpdatedPlaylist = updatedPlaylist;
            PlayingTrack = playingTrack;
            IndexOfPlayingTrack = (playingTrack == null) ? -1 : updatedPlaylist.Soundtracks.FindIndex(x => x.GUID == playingTrack.GUID);
        }
    }
}
