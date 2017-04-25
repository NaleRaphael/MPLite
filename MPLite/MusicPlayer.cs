using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;   //DllImport

namespace MPLite
{
    using TrackInfo = Core.TrackInfo;
    using Playlist = Core.Playlist;
    using PlaylistCollection = Core.PlaylistCollection;
    using PlayTrackEventArgs = Core.PlayTrackEventArgs;
    using PlaybackMode = Core.PlaybackMode;
    using TrackStatus = Core.TrackStatus;
    using InvalidPlaylistException = Core.InvalidPlaylistException;

    public class MusicPlayer
    {
        #region Field
        private Random randomNumber = new Random();

        private StringBuilder msg;  // MCI Error message
        private StringBuilder returnData;  // MCI return data
        private int error;

        //private Queue<int> trackQueue;
        //private CircularQueue<int> trackQueue;
        private TrackQueue trackQueue;

        // String that holds the MCI command
        // format: "s1 s2 s3 s4"
        // s1: 1st command(necessary), see also http://goo.gl/P39rDs
        // s2: file's name
        // s3: 2nd command
        // s4: 3rd command
        #endregion

        #region Properties
        public Playlist CurrPlaylist { get; set; }
        public TrackInfo PrevTrack { get; set; }
        public TrackInfo CurrentTrack { get; set; }
        public int CurrentTrackLength;
        public int CurrentTrackIndex { get; set; }
        public TrackStatus CurrentTrackStatus { get; set; }
        public int PrevTrackIndex { get; set; }
        public enum PlaybackState { Stopped = 0, Paused, Playing };
        public PlaybackState PlayerStatus = PlaybackState.Stopped;
        public PlaybackMode PlaybackMode = (PlaybackMode)Properties.Settings.Default.PlaybackMode;
        #endregion

        #region Event
        //public delegate void PlayerStoppedEventHandler(PlayTrackEventArgs e);
        //public event PlayerStoppedEventHandler PlayerStoppedEvent;
        //public delegate void PlayerStartedEventHandker(PlayTrackEventArgs e);
        //public event PlayerStartedEventHandker PlayerStartedEvent;
        //public delegate void PlayerPausedEventHandler();
        //public event PlayerPausedEventHandler PlayerPausedEvent;
        //public delegate void TrackEndsEventHandler(PlayTrackEventArgs e);
        //public event TrackEndsEventHandler TrackEndsEvent;
        //public delegate void MissingTrackEventHandler(PlayTrackEventArgs e);

        public delegate void PlayerStartedEventHandler(TrackInfo track);
        public event PlayerStartedEventHandler PlayerStartedEvent;
        public delegate void PlayerPausedEventHandler();
        public event PlayerPausedEventHandler PlayerPausedEvent;
        public delegate void PlayerStoppedEventHandler();
        public event PlayerStoppedEventHandler PlayerStoppedEvent;
        public delegate void TrackEndsEventHandler();
        public event TrackEndsEventHandler TrackEndsEvent;
        #endregion

        #region MCI API calls
        /// <summary>
        /// --- Parameters ---
        /// strCommand: Pointer to a null-terminated string that specifies an MCI command string. For a list, see Multimedia Command Strings.
        /// strReturn: Pointer to a buffer that receives return information. If no return information is needed, this parameter can be NULL.
        /// ReturnLength: Size, in characters, of the return buffer specified by the lpszReturnString parameter.
        /// hwndCallback: Handle to a callback window if the "notify" flag was specified in the command string.
        /// --- Returns ---
        /// (see [MCIERR Return Values]: http://goo.gl/fZO7Rg)
        /// (see [Status Command]: http://goo.gl/VVBvl1)
        /// </summary>
        [DllImport("winmm.dll")]
        private static extern int mciSendString(string strCommand, StringBuilder strReturn, int ReturnLength, IntPtr hwndCallback);

        [DllImport("winmm.dll")]
        private static extern int mciSendStringW(string strCommand, StringBuilder strReturn, int ReturnLength, IntPtr hwndCallback);

        /// <summary>
        /// errCode: Error code returned by the mciSendCommand or mciSendString function.
        /// errMsg: Pointer to a buffer that receives a null-terminated string describing the specified error.
        /// buflen: Length of the buffer, in characters, pointed to by the lpszErrorText parameter.
        /// </summary>
        [DllImport("winmm.dll")]
        public static extern int mciGetErrorString(int errCode, StringBuilder errMsg, int buflen);
        #endregion

        #region Constructer
        public MusicPlayer()
        {
            PlayerStatus = PlaybackState.Stopped;
            msg = new StringBuilder(128);
            returnData = new StringBuilder(128);
            PrevTrackIndex = -1;
            CurrentTrackIndex = -1;
        }
        #endregion

        #region Player action
        public void Close()
        {
            string cmd = "close MediaFile";
            mciSendString(cmd, null, 0, IntPtr.Zero);
        }

        public bool Open(PlayTrackEventArgs e)
        {
            Close();
            string cmd = "open \"" + e.CurrTrack.TrackPath + "\" type mpegvideo alias MediaFile";
            error = mciSendString(cmd, msg, 0, IntPtr.Zero);

            if (error == 277)
                throw new FailedToOpenFileException("There might be some unacceptable characters " +
                    "in the path of this file, you can rename it and try again.");

            if (error != 0)
            {
                // Cannot open file in the format ".mpeg", let MCI decide the file extension itself
                cmd = "open \"" + e.CurrTrack.TrackPath + "\"alias Mediafile";
                error = mciSendString(cmd, msg, 0, IntPtr.Zero);
                if (error == 305)
                    throw new InvalidFilePathException("Cannot find this file. Maybe it was moved to another path.");
                return (error == 0) ? true : false;
            }
            else return true;
        }

        public bool Open(TrackInfo track)
        {
            Close();
            string cmd = "open \"" + track.TrackPath + "\" type mpegvideo alias MediaFile";
            error = mciSendString(cmd, msg, 0, IntPtr.Zero);

            if (error == 277)
                throw new FailedToOpenFileException("There might be some unacceptable characters " +
                    "in the path of this file, you can rename it and try again.");

            if (error != 0)
            {
                // Cannot open file in the format ".mpeg", let MCI decide the file extension itself
                cmd = "open \"" + track.TrackPath + "\"alias Mediafile";
                error = mciSendString(cmd, msg, 0, IntPtr.Zero);
                if (error == 305)
                    throw new InvalidFilePathException("Cannot find this file. Maybe it was moved to another path.");
                return (error == 0) ? true : false;
            }
            else return true;
        }

        //public bool Play(PlayTrackEventArgs e)
        //{
        //    bool trackIsOpened = false;
        //    CurrentTrack = e.CurrTrack;
        //    CurrentTrackIndex = e.CurrTrackIndex;
        //    PlaybackMode = e.PlaybackMode;
        //    Properties.Settings.Default.TaskPlaylist = e.PlaylistName;
        //    Properties.Settings.Default.Save();

        //    try
        //    {
        //        trackIsOpened = Open(e);
        //    }
        //    catch (FailedToOpenFileException ex_FailedToOpen)  // Unacceptable chars in file path
        //    {
        //        throw ex_FailedToOpen;
        //    }
        //    catch (InvalidFilePathException ex_InvaildPath)
        //    {
        //        this.CurrentTrackStatus = TrackStatus.IncorrectPath;
        //        e.CurrTrackStatus = this.CurrentTrackStatus;

        //        throw ex_InvaildPath;
        //    }

        //    if (trackIsOpened)
        //    {
        //        string cmd = "play MediaFile";
        //        error = mciSendString(cmd, null, 0, IntPtr.Zero);

        //        if (error == 0)
        //        {
        //            // Save the trackInfo that is playing currently
        //            PlayerStatus = PlaybackState.Playing;

        //            this.CurrentTrackStatus = TrackStatus.Playing;
        //            e.CurrTrackStatus = this.CurrentTrackStatus;

        //            // Fire event to notify subscribers
        //            PlayerStartedEvent(e);
        //            return true;
        //        }
        //        else
        //        {
        //            Close();
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        // TODO: Exception handling
        //        return false;
        //    }
        //}

        public bool Play(TrackInfo track)
        {
            bool trackIsOpened = false;
            if (this.IsPlaying())
                this.Stop();

            if (track == null)
            {
                Console.WriteLine("No track is avalible.");
                PlayerStoppedEvent();
                return false;
            }

            try
            {
                trackIsOpened = Open(track);
            }
            catch (FailedToOpenFileException ex_FailedToOpen)  // Unacceptable chars in file path
            {
                throw ex_FailedToOpen;
            }
            catch (InvalidFilePathException ex_InvaildPath)
            {
                /*
                this.CurrentTrackStatus = TrackStatus.IncorrectPath;
                e.CurrTrackStatus = this.CurrentTrackStatus;
                */
                throw ex_InvaildPath;
            }

            if (trackIsOpened)
            {
                string cmd = "play MediaFile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);

                if (error == 0)
                {
                    // Save the trackInfo that is playing currently
                    PlayerStatus = PlaybackState.Playing;

                    //this.CurrentTrackStatus = TrackStatus.Playing;
                    //e.CurrTrackStatus = this.CurrentTrackStatus;

                    // Fire event to notify subscribers
                    PlayerStartedEvent(track);
                    return true;
                }
                else
                {
                    Close();
                    return false;
                }
            }
            else
            {
                // TODO: Exception handling
                return false;
            }
        }
        /*
        public void Pause()
        {
            if (PlayerStatus == PlaybackState.Paused)
            {
                Resume();
                PlayerStatus = PlaybackState.Playing;
                
                // Fire event
                PlayTrackEventArgs e = new PlayTrackEventArgs(Properties.Settings.Default.TaskPlaylist, -1,
                    CurrentTrack, (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode);
                e.CurrTrack = CurrentTrack;
                e.CurrTrackStatus = TrackStatus.Paused;
                PlayerStartedEvent(e);
            }
            else if (PlayerStatus == PlaybackState.Playing)
            {
                string cmd = "pause MediaFile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                PlayerStatus = PlaybackState.Paused;
                
                // Fire event
                PlayerPausedEvent();
            }
        }*/

        public void Pause()
        {
            if (PlayerStatus == PlaybackState.Paused)
            {
                Resume();
                PlayerStatus = PlaybackState.Playing;

                // Fire event
                PlayerStartedEvent(trackQueue.GetCurrentTrack());
            }
            else if (PlayerStatus == PlaybackState.Playing)
            {
                string cmd = "pause MediaFile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                PlayerStatus = PlaybackState.Paused;

                // Fire event
                PlayerPausedEvent();
            }
        }
        /*
        public void Stop(PlayTrackEventArgs e = null)
        {
            string cmd = "stop MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Stopped;

            this.CurrentTrackStatus = TrackStatus.Stopped;
            if (e != null)
                e.CurrTrackStatus = this.CurrentTrackStatus;
            Close();

            // Reset the info of playing track
            PrevTrack = CurrentTrack;
            CurrentTrack = null;
            
            // Fire event to notify subscribers
            PlayerStoppedEvent(e);
        }*/

        public void Stop()
        {
            string cmd = "stop MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Stopped;

            Close();

            // Fire event to notify subscribers
            PlayerStoppedEvent();
        }
        /*
        public void Resume()
        {
            string cmd = "play MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Playing;

            // Fire event
            PlayTrackEventArgs e = new PlayTrackEventArgs(Properties.Settings.Default.TaskPlaylist, -1,
                CurrentTrack, (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode);
            e.CurrTrack = CurrentTrack;
            e.CurrTrackStatus = TrackStatus.Playing;
            PlayerStartedEvent(e);
        }*/
        public void Resume()
        {
            string cmd = "play MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Playing;

            // Fire event
            PlayerStartedEvent(trackQueue.GetCurrentTrack());
        }
        #endregion

        #region Player status
        public bool IsPlaying()
        {
            string cmd = "status MediaFile mode";
            error = mciSendString(cmd, returnData, 128, IntPtr.Zero);
            if (returnData.Length == 7 && returnData.ToString().Substring(0, 7) == "playing")
                return true;
            else
                return false;
        }

        public bool IsOpen()    //status command: mode, seems there is no status "Open"
        {
            string cmd = "status MediaFile mode";
            error = mciSendString(cmd, returnData, 128, IntPtr.Zero);
            if (returnData.Length == 4 && returnData.ToString().Substring(0, 4) == "open")
                return true;
            else
                return false;
        }

        public bool IsPaused()
        {
            string cmd = "status MediaFile mode";
            error = mciSendString(cmd, returnData, 128, IntPtr.Zero);
            if (returnData.Length == 6 && returnData.ToString().Substring(0, 6) == "paused")
                return true;
            else
                return false;
        }

        public bool IsStopped()
        {
            string cmd = "status MediaFile mode";
            error = mciSendString(cmd, returnData, 128, IntPtr.Zero);
            if (returnData.Length == 7 && returnData.ToString().Substring(0, 7) == "stopped")
                return true;
            else
                return false;
        }
        #endregion

        #region Player logic
        public int GetCurrentMilisecond()
        {
            string cmd = "status MediaFile position";
            error = mciSendString(cmd, returnData, returnData.Capacity, IntPtr.Zero);

            // mciSendString will be failed if the track has ended with returning error code 263.
            // (NOTE: Timer cannot always be triggered at the end of the song)
            int current = (error == 0) ? int.Parse(returnData.ToString()) : 0;

            if (current >= CurrentTrackLength)
            {
                //this.CurrentTrackStatus = TrackStatus.Stopped;
                //PlaybackMode mode = (PlaybackMode)Properties.Settings.Default.TaskPlaybackMode;
                //TrackEndsEvent(new PlayTrackEventArgs(CurrPlaylist.ListName, CurrentTrackIndex, null, mode, this.CurrentTrackStatus));
                TrackEndsEvent();
                return 0;
            }
            return current;
        }

        public void SetPosition(int milisecond)
        {
            if (IsPlaying())
            {
                string cmd = "play MediaFile from " + milisecond.ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
            }
            else
            {
                string cmd = "seek MediaFile to " + milisecond.ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
            }
        }

        public int GetSongLength()
        {
            string cmd = "status MediaFile length";
            error = mciSendString(cmd, returnData, returnData.Capacity, IntPtr.Zero);
            CurrentTrackLength = int.Parse(returnData.ToString());
            return CurrentTrackLength;
        }
        #endregion

        #region Audio control
        public bool SetVolume(int volume)
        {
            if (volume >= 0 && volume <= 1000)
            {
                string cmd = "setaudio MediaFile volume to " + volume.ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetBalance(int balance, int volume)
        {
            if (balance >= 0 && balance <= 2000)
            {
                double volPercent = (double)volume / 1000;
                double dBalance = volPercent * balance;
                string cmd = "setaudio MediaFile left volume to " + ((int)(balance * volPercent)).ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                cmd = "setaudio MediaFile right volume to " + ((int)((2000 - dBalance) * volPercent)).ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                return true;
            }
            return false;
        }
        #endregion

        // Called by PagePlaylist. Because it needs to know the index of playing track to set playing sign.
        public PlayTrackEventArgs GetNextTrack(string playlistName, int selectedIdx, PlaybackMode mode, out int trackIdx)
        {
            if (trackQueue == null)
            {
                Playlist pl = PlaylistCollection.GetDatabase().TrackLists.Find(x => x.ListName == playlistName);
                if (pl == null || pl.TrackAmount == 0)
                    throw new InvalidPlaylistException(string.Format("Given playlist {0} is invalid.", playlistName));
                trackQueue = new TrackQueue(pl, selectedIdx, mode);
            }

            TrackInfo currentTrack;
            int currentTrackIdx = trackQueue.Current(out currentTrack);
            TrackInfo nextTrack;
            trackIdx = trackQueue.Next(out nextTrack);

            PlayTrackEventArgs e = new PlayTrackEventArgs(trackQueue.ListName, currentTrackIdx, currentTrack, trackQueue.Mode, this.CurrentTrackStatus);
            e.SetNextTrack(nextTrack, trackIdx);

            return e;

            /*Playlist pl = PlaylistCollection.GetDatabase().TrackLists.Find(x => x.ListName == playlistName);
            if (pl == null || pl.TrackAmount == 0)
                throw new InvalidPlaylistException(string.Format("Given playlist {0} is invalid.", playlistName));

            trackIdx = GetTrackIdxFromQueue(pl, selectedIdx, mode, true);
            TrackInfo track = (trackIdx == -1) ? null : CurrPlaylist.Soundtracks[trackIdx];
            
            PlayTrackEventArgs e = new PlayTrackEventArgs(playlistName, this.CurrentTrackIndex, this.CurrentTrack, mode, this.CurrentTrackStatus);
            e.SetNextTrack(track, trackIdx);
            return e;*/
        }

        public TrackInfo GetTrack(string listName, int selIdx, PlaybackMode mode)
        {
            if (trackQueue == null)
            {
                Playlist pl = PlaylistCollection.GetDatabase().TrackLists.Find(x => x.ListName == listName);
                if (pl == null || pl.TrackAmount == 0)
                    throw new InvalidPlaylistException(string.Format("Given playlist {0} is invalid.", listName));
                trackQueue = new TrackQueue(pl, selIdx, mode);
            }

            TrackInfo nextTrack;
            int nextTrackIdx = trackQueue.Next(out nextTrack);

            return nextTrack;
        }

        public TrackInfo GetPrevTrack(string listName, int selIdx, PlaybackMode mode)
        {
            TrackInfo prevTrack;
            int prevTrackIdx = trackQueue.Previous(out prevTrack);

            return prevTrack;
        }

        public void ClearQueue()
        {
            if (trackQueue != null)
            {
                trackQueue.Clear();
                trackQueue = null;
            }
        }
    }

    public class FailedToOpenFileException : Exception
    {
        public FailedToOpenFileException(string message) : base(message)
        {
        }
    }

    public class InvalidFilePathException : Exception
    {
        public InvalidFilePathException(string message) : base(message)
        {
        }
    }
}
