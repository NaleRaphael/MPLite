using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;   //DllImport
using System.Windows.Forms;

namespace MPLite
{
    public class MusicPlayer
    {
        #region Field
        private Random randomNumber = new Random();

        private StringBuilder msg;  // MCI Error message
        private StringBuilder returnData;  // MCI return data
        private int error;

        private Queue<int> trackQueue;
        //private TrackQueue<int> trackQueue;

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
        public int CurrentTrackNum { get; set; }
        public enum PlaybackState { Stopped = 0, Paused, Playing };
        public PlaybackState PlayerStatus = PlaybackState.Stopped;
        //public bool Loop { get; set; }
        //public bool Shuffle { get; set; }
        public MPLiteConstant.PlaybackMode PlaybackMode = (MPLiteConstant.PlaybackMode)Properties.Settings.Default.PlaybackMode;
        #endregion

        #region Event
        public delegate void PlayerStoppedEventHandler(TrackInfo track);
        public event PlayerStoppedEventHandler PlayerStoppedEvent;
        public delegate void PlayerStartedEventHandker(TrackInfo track);
        public event PlayerStartedEventHandker PlayerStartedEvent;
        public delegate void PlayerPausedEventHandler();
        public event PlayerPausedEventHandler PlayerPausedEvent;
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

        public MusicPlayer()
        {
            PlayerStatus = PlaybackState.Stopped;
            msg = new StringBuilder(128);
            returnData = new StringBuilder(128);
        }

        #region Buttons
        public void Close()
        {
            string cmd = "close MediaFile";
            mciSendString(cmd, null, 0, IntPtr.Zero);
        }

        public bool Open(TrackInfo track)
        {
            Close();
            string cmd = "open \"" + track.TrackPath + "\" type mpegvideo alias MediaFile";
            error = mciSendString(cmd, msg, 0, IntPtr.Zero);

            if (error == 277)
                throw new FailedToOpenFileException("There might be some inacceptable characters " +
                    "in the path of this file, you can rename it and try again.");

            if (error != 0)
            {
                // Cannot open file in the format ".mpeg", let MCI decide the file extension itself
                cmd = "open \"" + track.TrackPath + "\"alias Mediafile";
                error = mciSendString(cmd, msg, 0, IntPtr.Zero);
                return (error == 0) ? true : false;
            }
            else return true;
        }

        public bool Play(TrackInfo track)
        {
            bool trackIsOpened = false;
            try
            {
                trackIsOpened = Open(track);
            }
            catch
            {
                throw;
            }

            if (trackIsOpened)
            {
                string cmd = "play MediaFile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);

                if (error == 0)
                {
                    // Save the trackInfo that is playing currently
                    CurrentTrack = track;
                    PlayerStatus = PlaybackState.Playing;

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

        public void Pause()
        {
            if (PlayerStatus == PlaybackState.Paused)
            {
                Resume();
                PlayerStatus = PlaybackState.Playing;
                // Fire event
                PlayerStartedEvent(CurrentTrack);
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

        public void Stop()
        {
            string cmd = "stop MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Stopped;
            Close();

            // Reset the info of playing track
            PrevTrack = CurrentTrack;
            CurrentTrack = null;

            // Fire event to notify subscribers
            PlayerStoppedEvent(PrevTrack);
        }

        public void Resume()
        {
            string cmd = "play MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Playing;

            // Fire event
            PlayerStartedEvent(CurrentTrack);
        }
        #endregion

        #region Status
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

        #region Logic
        public int GetCurrentMilisecond()
        {
            string cmd = "status MediaFile position";
            error = mciSendString(cmd, returnData, returnData.Capacity, IntPtr.Zero);

            // mciSendString will be failed if the track has ended with returning error code 263.
            // (NOTE: Timer cannot always be triggered at the end of the song)
            int current = (error == 0) ? int.Parse(returnData.ToString()) : 0;

            if (current >= CurrentTrackLength)
            {
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

        #region Audio
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
        public TrackInfo GetNextTrack(out int trackIdx)
        {
            if (CurrPlaylist == null)
                throw new InvalidPlaylistException("No selected playlist");

            trackIdx = GetTrackIdxFromQueue(CurrPlaylist, 0,
                (MPLiteConstant.PlaybackMode)Properties.Settings.Default.PlaybackMode);

            return (trackIdx == -1) ? null : CurrPlaylist.Soundtracks[trackIdx];
        }

        public TrackInfo GetNextTrack(Playlist playlist, int selectedIdx, out int trackIdx)
        {
            if (playlist.TrackAmount <= 0)
                throw new EmptyPlaylistException("Playlist is empty");

            trackIdx = GetTrackIdxFromQueue(playlist, selectedIdx,
                (MPLiteConstant.PlaybackMode)Properties.Settings.Default.PlaybackMode);

            return (trackIdx == -1) ? null : CurrPlaylist.Soundtracks[trackIdx];
        }

        public TrackInfo GetNextTrack(string playlistName, int selectedIdx, out int trackIdx)
        {
            Playlist pl = PlaylistCollection.GetDatabase().TrackLists.Find(x => x.ListName == playlistName);
            if (pl == null || pl.TrackAmount == 0)
                throw new InvalidPlaylistException(string.Format("Given playlist {0} is invalid.", playlistName));

            trackIdx = GetTrackIdxFromQueue(pl, selectedIdx, 
                (MPLiteConstant.PlaybackMode)Properties.Settings.Default.PlaybackMode);

            return (trackIdx == -1) ? null : CurrPlaylist.Soundtracks[trackIdx];
        }

        private int GetTrackIdxFromQueue(Playlist playlist, int beginningIdx, MPLiteConstant.PlaybackMode mode)
        {
            int trackIdx = 0;
            if (trackQueue == null || playlist.ListName != CurrPlaylist.ListName)
            {
                CurrPlaylist = playlist;
                InitTrackQueue(playlist.TrackAmount, beginningIdx, mode);
                //trackIdx = trackQueue.Dequeue();
            }
            switch (mode)
            {
                case MPLiteConstant.PlaybackMode.Default:
                case MPLiteConstant.PlaybackMode.ShuffleOnce:
                    if (trackQueue.Count > 0)
                    {
                        trackIdx = trackQueue.Dequeue();
                    }
                    else
                    {
                        trackIdx = -1;
                        trackQueue = null;
                    }
                    break;
                case MPLiteConstant.PlaybackMode.PlaySingle:
                    // No need to enqueue again
                    trackIdx = (trackQueue.Count > 0) ? trackQueue.Dequeue() : -1;
                    break;
                case MPLiteConstant.PlaybackMode.RepeatTrack:
                case MPLiteConstant.PlaybackMode.RepeatList:
                case MPLiteConstant.PlaybackMode.Shuffle:
                    trackIdx = trackQueue.Dequeue();
                    trackQueue.Enqueue(trackIdx);
                    break;
            }
            return trackIdx;
        }

        private void InitTrackQueue(int trackAmount, int beginningIdx, MPLiteConstant.PlaybackMode mode)
        {
            if (trackQueue != null)
            {
                trackQueue.Clear();
                trackQueue = null;
            }

            // Reset beginningIdx if no track is selected
            beginningIdx = (beginningIdx == -1) ? 0 : beginningIdx;
            switch (mode)
            {
                case MPLiteConstant.PlaybackMode.Default:
                case MPLiteConstant.PlaybackMode.RepeatList:
                    trackQueue = new Queue<int>(trackAmount);
                    for (int i = beginningIdx; i < trackAmount; i++)
                    {
                        trackQueue.Enqueue(i);
                    }
                    break;
                case MPLiteConstant.PlaybackMode.Shuffle:
                case MPLiteConstant.PlaybackMode.ShuffleOnce:
                    // TODO: improve this
                    Random rand = new Random();
                    trackQueue = new Queue<int>(trackAmount);
                    int[] ary = new int[trackAmount-1];

                    // Initialize array
                    for (int i = 0; i < beginningIdx; i++)
                    {
                        ary[i] = i;
                    }
                    for (int i = beginningIdx + 1; i < trackAmount; i++)
                    {
                        ary[i-1] = i;
                    }

                    // Swap randomly
                    for (int i = 0; i < trackAmount - 1; i++)
                    {
                        int tempIdx = rand.Next(trackAmount - 1);
                        int temp = ary[tempIdx];
                        ary[tempIdx] = ary[i];
                        ary[i] = temp;
                    }

                    trackQueue.Enqueue(beginningIdx);
                    for (int i = 0; i < trackAmount-1; i++)
                    {
                        trackQueue.Enqueue(ary[i]);
                    }
                    break;
                case MPLiteConstant.PlaybackMode.PlaySingle:
                case MPLiteConstant.PlaybackMode.RepeatTrack:
                    trackQueue = new Queue<int>(1);
                    trackQueue.Enqueue(beginningIdx);
                    break;
                default:
                    break;
            }
        }

        public void ResetQueue()
        {
            trackQueue = null;
        }
    }

    public class FailedToOpenFileException : Exception
    {
        public FailedToOpenFileException(string message) : base(message)
        {
        }
    }
}
