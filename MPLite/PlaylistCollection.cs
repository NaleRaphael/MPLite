using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
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
        public static void Update(string[] filePaths, string selectedPlaylist)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath);

            if (plc == null)
            {
                Playlist pl = new Playlist(selectedPlaylist);
                pl.UpdateTracks(filePaths);
                plc = new PlaylistCollection();
                plc.TrackLists.Add(pl);
            }
            else
            {
                Playlist pl = plc.TrackLists.Find(x => x.ListName == selectedPlaylist);
                pl.UpdateTracks(filePaths);
            }
            DataControl.SaveData<PlaylistCollection>(configPath, plc);
        }

        public static void Update(List<TrackInfo> tracks, string selectedPlaylist)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath);

            if (plc == null)
            {
                Playlist pl = new Playlist(selectedPlaylist);
                pl.UpdateTracks(tracks);
                plc = new PlaylistCollection();
                plc.TrackLists.Add(pl);
            }
            else
            {
                Playlist pl = plc.TrackLists.Find(x => x.ListName == selectedPlaylist);
                pl.UpdateTracks(tracks);
            }
            DataControl.SaveData<PlaylistCollection>(configPath, plc);
        }

        public static void DeleteTracksByIndices(int[] indices, string selectedPlaylist)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath);

            if (plc == null)
            {
                throw new EmptyJsonFileException("No track can be deleted from database. Please check your config file.");
            }

            Playlist pl = plc.TrackLists.Find(x => x.ListName == selectedPlaylist);
            pl.DeleteTracksByIndices(indices);

            DataControl.SaveData<PlaylistCollection>(configPath, plc);
        }

        public static PlaylistCollection GetDatabase()
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            return DataControl.ReadFromJson<PlaylistCollection>(configPath);
        }

        public static Playlist GetPlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            Playlist pl;
            try
            {
                pl = DataControl.ReadFromJson<PlaylistCollection>(configPath).TrackLists.Find(x => x.ListName == listName);
            }
            catch
            {
                throw;
            }
            return pl;
        }

        public static string AddPlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath);
                string newlistName = AddSerialNum(plc.TrackLists, listName);
                Playlist pl = new Playlist(newlistName);
                plc.TrackLists.Add(pl);
                DataControl.SaveData<PlaylistCollection>(configPath, plc);

                return newlistName;
            }
            catch
            {
                throw;
            }
        }

        public static void RemovePlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            try
            {
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath);
                plc.TrackLists.Remove(plc.TrackLists.Find(x => x.ListName == listName));
                DataControl.SaveData<PlaylistCollection>(configPath, plc);
            }
            catch
            {
                throw;
            }
        }

        private static string AddSerialNum(List<Playlist> collection, string target)
        {
            int serialNum = 0;
            string temp = target;

            while(collection.Find(x => x.ListName == temp) != null)
            {
                serialNum++;
                temp = target + serialNum.ToString();
            }

            if (serialNum == 0)
            {
                return target;
            }
            else
            {
                return target + serialNum.ToString();
            }
        }
    }

    public class Playlist
    {
        public string ListName { get; set; }
        public List<TrackInfo> Soundtracks { get; set; }
        public int TotalTracks { get { return Soundtracks.Count; } }

        public Playlist(string listName)
        {
            ListName = listName;
            Soundtracks = new List<TrackInfo>();
        }

        public void UpdateTracks(string[] filePaths)
        {
            foreach (string filePath in filePaths)
            {
                string trackName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                //TrackInfo trackInfo = new TrackInfo { TrackName = trackName, TrackPath = filePath };
                //this.Soundtracks.Add(trackInfo);
                this.Soundtracks.Add(TrackInfo.ParseSource(filePath));
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

        public void MoveTrack(int oriIdx, int newIdx)
        {
            TrackInfo track = Soundtracks[oriIdx];
            Soundtracks.RemoveAt(oriIdx);
            Soundtracks.Insert(newIdx, track);
        }

        // TODO: move multiple tracks
    }

    public class InvalidPlaylistException : Exception
    {
        public InvalidPlaylistException(string message) : base(message)
        {
        }
    }
}
