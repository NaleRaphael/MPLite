using System;
using System.Collections.Generic;

namespace MPLite.Core
{
    public class PlaylistCollection
    {
        public List<Playlist> TrackLists { get; set; }

        public PlaylistCollection()
        {
            TrackLists = new List<Playlist>();
        }

        // TODO: Update by given playlist? (necessary?)

        // TODO: Update listview then update database (according to the order of playlist)
        public static void AddPlaylist(string[] filePaths, string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);

            if (plc == null)
            {
                Playlist pl = new Playlist(listName);
                pl.UpdateTracks(filePaths);
                plc = new PlaylistCollection();
                plc.TrackLists.Add(pl);
            }
            else
            {
                Playlist pl = plc.TrackLists.Find(x => x.ListName == listName);
                pl.UpdateTracks(filePaths);
            }
            DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
        }

        public static void AddPlaylist(List<TrackInfo> tracks, string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);

            if (plc == null)
            {
                Playlist pl = new Playlist(listName);
                pl.UpdateTracks(tracks);
                plc = new PlaylistCollection();
                plc.TrackLists.Add(pl);
            }
            else
            {
                Playlist pl = plc.TrackLists.Find(x => x.ListName == listName);
                pl.UpdateTracks(tracks);
            }
            DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
        }

        public static Playlist UpdatePlaylist(List<TrackInfo> tracks, string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
            Playlist pl = null;

            if (plc == null)
            {
                pl = new Playlist(listName);
                pl.UpdateTracks(tracks);
                plc = new PlaylistCollection();
                plc.TrackLists.Add(pl);
            }
            else
            {
                pl = plc.TrackLists.Find(x => x.ListName == listName);
                pl.UpdateTracks(tracks);
            }
            DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);

            return pl;
        }

        public static void DeleteTracksByIndices(int[] indices, string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);

            if (plc == null)
            {
                throw new EmptyJsonFileException("No track can be deleted from database. Please check your config file.");
            }

            Playlist pl = plc.TrackLists.Find(x => x.ListName == listName);
            pl.DeleteTracksByIndices(indices);

            DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
        }

        public static void DeleteTracksByIndices(int[] indices, Guid guid)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);

            if (plc == null)
            {
                throw new EmptyJsonFileException("No track can be deleted from database. Please check your config file.");
            }

            Playlist pl = plc.TrackLists.Find(x => x.GUID == guid);
            pl.DeleteTracksByIndices(indices);

            DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
        }

        public static PlaylistCollection GetDatabase()
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            return DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
        }

        public void SaveToDatabase()
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            DataControl.SaveData<PlaylistCollection>(configPath, this, false, true);
        }

        public static Playlist GetPlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            Playlist pl;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
                pl = (plc == null) ? null : plc.TrackLists.Find(x => x.ListName == listName);
            }
            catch
            {
                throw;
            }
            return pl;
        }

        public static Playlist GetPlaylist(Guid guid)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            Playlist pl;
            try
            {
                PlaylistCollection plc = GetDatabase();
                if (plc == null)
                    return null;
                pl = plc.TrackLists.Find(x => x.GUID == guid);
            }
            catch
            {
                throw;
            }
            return pl;
        }

        public static string AddPlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
                plc = (plc == null) ? new PlaylistCollection() : plc;

                string newlistName = AddSerialNum(plc.TrackLists, listName);
                Playlist pl = new Playlist(newlistName);
                plc.TrackLists.Add(pl);
                DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);

                return newlistName;
            }
            catch
            {
                throw;
            }
        }

        public static void AddPlaylist(Playlist pl)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
                plc = (plc == null) ? new PlaylistCollection() : plc;
                plc.TrackLists.Add(pl);
                DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
            }
            catch
            {
                throw;
            }
        }

        public static Playlist ReorderTracks(Guid listGUID, List<int> selectedIndices, int insertIndex)
        {
            PlaylistCollection plc = GetDatabase();
            Playlist pl = plc.TrackLists.Find(x => x.GUID == listGUID);

            if (pl.ReorderTracks(selectedIndices, insertIndex))
            {
                plc.SaveToDatabase();
                return pl;
            }
            else return null;   // Failed to reorder tracks (no need to reorder tracks)
        }

        public static void RemovePlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
                plc.TrackLists.Remove(plc.TrackLists.Find(x => x.ListName == listName));
                DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
            }
            catch
            {
                throw;
            }
        }

        public static void RemovePlaylist(Guid targetGUID)
        {
            string configPath = Properties.Settings.Default.TrackDBPath;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, true);
                plc.TrackLists.RemoveAt(plc.TrackLists.FindIndex(x => x.GUID == targetGUID));
                DataControl.SaveData<PlaylistCollection>(configPath, plc, false, true);
            }
            catch
            {
                throw;
            }
        }

        public static string AddSerialNum(List<Playlist> collection, string listName)
        {
            int serialNum = 0;
            string temp = listName;

            while (collection.Find(x => x.ListName == temp) != null)
            {
                serialNum++;
                temp = listName + serialNum.ToString();
            }

            if (serialNum == 0)
            {
                return listName;
            }
            else
            {
                return listName + serialNum.ToString();
            }
        }
    }

    public class Playlist
    {
        public Guid GUID { get; set; }
        public string ListName { get; set; }
        public List<TrackInfo> Soundtracks { get; set; }
        public int TrackAmount { get { return Soundtracks.Count; } }

        public Playlist(string listName)
        {
            GUID = Guid.NewGuid();
            ListName = listName;
            Soundtracks = new List<TrackInfo>();
        }

        public void UpdateTracks(string[] filePaths)
        {
            foreach (string filePath in filePaths)
            {
                string trackName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                this.Soundtracks.Add(Core.TrackInfo.ParseSource(filePath));
            }
        }

        public void UpdateTracks(List<TrackInfo> tracks)
        {
            foreach (TrackInfo track in tracks)
            {
                this.Soundtracks.Add(track);
            }
        }

        public void DeleteTracksByIndices(int[] indices)
        {
            Array.Sort<int>(indices, (x, y) => { return -x.CompareTo(y); });
            foreach (int i in indices)
            {
                Soundtracks.RemoveAt(i);
            }
        }

        public void InsertTracks(List<TrackInfo> tracks, int insertIdx)
        {
            foreach (TrackInfo track in tracks)
            {
                Soundtracks.Insert(insertIdx, track);
            }
        }

        public bool ReorderTracks(List<int> trackIdx, int insertIdx)
        {
            int firstIdx = trackIdx[0];
            int newInsertIdx = insertIdx;

            // correct insertIdx by checking how many items locating before it
            for (int i = 0; i < trackIdx.Count; i++)
            {
                if (trackIdx[i] < insertIdx)
                    newInsertIdx--;
            }

            if (newInsertIdx == firstIdx)
                return false;   // no need to reorder tracks
            else
            {
                trackIdx.Sort((x, y) => -x.CompareTo(y));   // descending sorting
                List<TrackInfo> tracks = new List<TrackInfo>(trackIdx.Count);
                for (int i = 0; i < trackIdx.Count; i++)
                {
                    tracks.Add(Soundtracks[trackIdx[i]]);
                    Soundtracks.RemoveAt(trackIdx[i]);
                }
                this.InsertTracks(tracks, newInsertIdx);
            }

            return true;
        }
    }

    public class InvalidPlaylistException : Exception
    {
        public InvalidPlaylistException(string message) : base(message)
        {
        }
    }

    public class PlayTrackEventArgs : EventArgs
    {
        public Guid PlaylistGUID { get; set; }
        public int PrevTrackIndex { get; set; }
        public int CurrTrackIndex { get; set; }
        public TrackInfo CurrTrack { get; set; }
        public TrackInfo PrevTrack { get; set; }
        public TrackStatus PrevTrackStatus { get; set; }
        public TrackStatus CurrTrackStatus { get; set; }
        public PlaybackMode PlaybackMode { get; set; }

        // Default value of playlistName should be `null` so that music play can selected playlist automatically.
        public PlayTrackEventArgs(Guid listGUID, int trackIdx = -1, TrackInfo track = null,
            PlaybackMode mode = PlaybackMode.None, 
            TrackStatus trackStatus = TrackStatus.None)
        {
            PlaylistGUID = (PlaylistGUID == null) ? Properties.Settings.Default.LastSelectedPlaylistGUID : listGUID;
            PrevTrack = null;
            CurrTrack = track;
            PrevTrackIndex = -1;
            CurrTrackIndex = trackIdx;
            PrevTrackStatus = TrackStatus.None;
            CurrTrackStatus = (trackStatus == TrackStatus.None) ? TrackStatus.None : trackStatus;
            PlaybackMode = (mode == PlaybackMode.None) ? 
                (PlaybackMode)Properties.Settings.Default.PlaybackMode : mode;

            // Use TaskPlaybackMode as a global variable to handle PlayTrackEvent whether it comes from user-clicked or scheduler-triggered.
            Properties.Settings.Default.TaskPlaybackMode = (int)PlaybackMode;
            Properties.Settings.Default.Save();
        }

        public void SetNextTrack(TrackInfo newTrack = null, int newTrackIdx = -1)
        {
            PrevTrack = CurrTrack;
            CurrTrack = newTrack;
            PrevTrackIndex = CurrTrackIndex;
            CurrTrackIndex = newTrackIdx;
            PrevTrackStatus = CurrTrackStatus;
            CurrTrackStatus = TrackStatus.None;
        }
    }
}
