using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;   //DllImport
using System.Windows.Forms;

namespace MPLite
{
    class MusicPlayer
    {
        #region Field
        private Random randomNumber = new Random();

        private StringBuilder msg;  // MCI Error message
        private StringBuilder returnData;  // MCI return data
        private int error;

        // String that holds the MCI command
        // format: "s1 s2 s3 s4"
        // s1: 1st command(necessary), see also http://goo.gl/P39rDs
        // s2: file's name
        // s3: 2nd command
        // s4: 3rd command
        #endregion

        #region Properties
        public TrackInfo CurrentTrack { get; set; }
        public int CurrentTrackNum { get; set; }
        public enum PlaybackState { Stopped = 0, Paused, Playing };
        public PlaybackState PlayerStatus = PlaybackState.Stopped;
        public bool Loop { get; set; }
        public bool Shuffle { get; set; }
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
            Loop = false;
            Shuffle = false;
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
            error = mciSendString(cmd, null, 0, IntPtr.Zero);

            if (error != 0)
            {
                // Cannot open file in the format ".mpeg", let MCI decide the file extension itself
                cmd = "open \"" + track.TrackPath + "\"alias Mediafile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                return (error == 0) ? true : false;
            }
            else return true;
        }

        public bool Play(TrackInfo track)
        {
            if (Open(track))
            {
                string cmd = "play MediaFile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);

                if (error == 0)
                {
                    CurrentTrack = track;
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
            }
            else if (PlayerStatus == PlaybackState.Playing)
            {
                string cmd = "pause MediaFile";
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                PlayerStatus = PlaybackState.Paused;
            }
        }

        public void Stop()
        {
            string cmd = "stop MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
            PlayerStatus = PlaybackState.Stopped;
            Close();
        }

        public void Resume()
        {
            string cmd = "resume MediaFile";
            error = mciSendString(cmd, null, 0, IntPtr.Zero);
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
            return int.Parse(returnData.ToString());
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
            if (IsPlaying())
            {
                string cmd = "status MediaFile length";
                error = mciSendString(cmd, returnData, returnData.Capacity, IntPtr.Zero);
                return int.Parse(returnData.ToString());
            }
            else
            {
                return 0;
            }
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
                string cmd = "setaudio MediaFile left volume to " + ((int)(balance*volPercent)).ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                cmd = "setaudio MediaFile right volume to " + ((int)((2000-dBalance)*volPercent)).ToString();
                error = mciSendString(cmd, null, 0, IntPtr.Zero);
                return true;
            }
            return false;
        }
        #endregion

        /*public int GetSong(bool previous)
        {
            if (Shuffle)
            {
                int i;
                if (playList.Items.Count == 1)
                    return 0;
                while (true)
                {
                    // Can be improve
                    i = randomNumber.Next(playList.Items.Count);
                    if (i != CurrentTrackNum)
                        return i;
                }
            }
            else if (Loop && !previous)
            {
                if (CurrentTrackNum == playList.Items.Count - 1)
                    return 0;
                else
                    return CurrentTrackNum + 1;
            }
            else if (Loop && previous)
            {
                if (CurrentTrackNum == 0)
                    return playList.Items.Count - 1;
                else
                    return CurrentTrackNum - 1;
            }
            else
            {
                if (previous)
                {
                    if (CurrentTrackNum != 0)
                        return CurrentTrackNum - 1;
                    else
                        return 0;
                }
                else
                {
                    if (CurrentTrackNum != playList.Items.Count - 1)
                        return CurrentTrackNum + 1;
                    else
                        return 0;
                }
            }
        }*/
    }
}
