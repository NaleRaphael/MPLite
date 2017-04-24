﻿using System;
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
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, false);

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
            DataControl.SaveData<PlaylistCollection>(configPath, plc, false);
        }

        public static void AddPlaylist(List<TrackInfo> tracks, string listName)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, false);

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
            DataControl.SaveData<PlaylistCollection>(configPath, plc, false);
        }

        public static void DeleteTracksByIndices(int[] indices, string listName)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, false);

            if (plc == null)
            {
                throw new EmptyJsonFileException("No track can be deleted from database. Please check your config file.");
            }

            Playlist pl = plc.TrackLists.Find(x => x.ListName == listName);
            pl.DeleteTracksByIndices(indices);

            DataControl.SaveData<PlaylistCollection>(configPath, plc, false);
        }

        public static void DeleteTracksByIndices(int[] indices, Guid guid)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, false);

            if (plc == null)
            {
                throw new EmptyJsonFileException("No track can be deleted from database. Please check your config file.");
            }

            Playlist pl = plc.TrackLists.Find(x => x.GUID == guid);
            pl.DeleteTracksByIndices(indices);

            DataControl.SaveData<PlaylistCollection>(configPath, plc, false);
        }

        public static PlaylistCollection GetDatabase()
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            return DataControl.ReadFromJson<PlaylistCollection>(configPath, false);
        }

        public void SaveToDatabase()
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            DataControl.SaveData<PlaylistCollection>(configPath, this, false);
        }

        public static Playlist GetPlaylist(string listName)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            Playlist pl;
            try
            {
                pl = DataControl.ReadFromJson<PlaylistCollection>(configPath, false).TrackLists.Find(x => x.ListName == listName);
            }
            catch
            {
                throw;
            }
            return pl;
        }

        public static Playlist GetPlaylist(Guid guid)
        {
            string configPath = Properties.Settings.Default.PlaylistInfoPath;
            Playlist pl;
            try
            {
                pl = DataControl.ReadFromJson<PlaylistCollection>(configPath, false).TrackLists.Find(x => x.GUID == guid);
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
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, false);
                plc = (plc == null) ? new PlaylistCollection() : plc;

                string newlistName = AddSerialNum(plc.TrackLists, listName);
                Playlist pl = new Playlist(newlistName);
                plc.TrackLists.Add(pl);
                DataControl.SaveData<PlaylistCollection>(configPath, plc, false);

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
                PlaylistCollection plc = DataControl.ReadFromJson<PlaylistCollection>(configPath, false);
                plc.TrackLists.Remove(plc.TrackLists.Find(x => x.ListName == listName));
                DataControl.SaveData<PlaylistCollection>(configPath, plc, false);
            }
            catch
            {
                throw;
            }
        }

        private static string AddSerialNum(List<Playlist> collection, string listName)
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

        public void MoveTrack(int oriIdx, int newIdx)
        {
            TrackInfo track = Soundtracks[oriIdx];
            Soundtracks.RemoveAt(oriIdx);
            Soundtracks.Insert(newIdx, track);
        }

        // TODO: move multiple tracks (dragging items in lv_Playlist)
    }

    public class InvalidPlaylistException : Exception
    {
        public InvalidPlaylistException(string message) : base(message)
        {
        }
    }

    public class PlayTrackEventArgs : EventArgs
    {
        public string PlaylistName { get; set; }
        public int PrevTrackIndex { get; set; }
        public int CurrTrackIndex { get; set; }
        public TrackInfo CurrTrack { get; set; }
        public TrackInfo PrevTrack { get; set; }
        public TrackStatus PrevTrackStatus { get; set; }
        public TrackStatus CurrTrackStatus { get; set; }
        public PlaybackMode PlaybackMode { get; set; }

        // Default value of playlistName should be `null` so that music play can selected playlist automatically.
        public PlayTrackEventArgs(string playlistName = null, int trackIdx = -1, TrackInfo track = null,
            PlaybackMode mode = PlaybackMode.None, 
            TrackStatus trackStatus = TrackStatus.None)
        {
            PlaylistName = (playlistName == null) ? Properties.Settings.Default.LastSelectedPlaylist : playlistName;
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