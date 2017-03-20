using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;

namespace MPLite
{
    public class MusicPlayer : Component
    {
        #region Properties
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;

        public TrackInfo CurrentTrack { get; set; }
        public MMDevice CurrentDevice { get; set; }
        public List<MMDevice> MMDeviceList;
        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;

        public PlaybackState PlaybackState
        {
            get
            {
                if (_soundOut != null)
                    return _soundOut.PlaybackState;
                else
                    return PlaybackState.Stopped;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetLength();
                else
                    return TimeSpan.Zero;
            }
        }

        public int Volume
        {
            get
            {
                if (_soundOut != null)
                    return Math.Min(100, Math.Max((int)(_soundOut.Volume * 100), 0));
                return 100;
            }

            set
            {
                if (_soundOut != null)
                    _soundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
            }
        }
        #endregion

        public MusicPlayer()
        {
            GetMMDevice();

            // Initialize MMDevice
            if (MMDeviceList.Count == 0)
            {
                throw new Exception("No connected device can be used to play tracks.");
            }

            // TODO: Make this selectable for user
            CurrentDevice = MMDeviceList[1];
        }

        private void GetMMDevice()
        {
            if (MMDeviceList != null)
                MMDeviceList.Clear();
            else
                MMDeviceList = new List<MMDevice>();

            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (var mmdeviceCollection = mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    foreach (var device in mmdeviceCollection)
                    {
                        MMDeviceList.Add(device);
                    }
                }
            }
        }

        // TODO: implement this
        public void SelectMMDevice()
        {
            throw new NotImplementedException();
        }

        public void Open(TrackInfo trackInfo)
        {
            CleanupPlayback();

            _waveSource =
                CodecFactory.Instance.GetCodec(trackInfo.TrackPath)
                    .ToSampleSource()
                    .ToMono()
                    .ToWaveSource();
            _soundOut = new WasapiOut() { Latency = 100, Device = CurrentDevice };
            _soundOut.Initialize(_waveSource);
            if (PlaybackStopped != null) _soundOut.Stopped += PlaybackStopped;
        }

        public void Play()
        {
            if (_soundOut != null)
                _soundOut.Play();
        }

        public void Pause()
        {
            if (_soundOut != null)
                _soundOut.Pause();
        }

        public void Stop()
        {
            if (_soundOut != null)
                _soundOut.Stop();
        }

        public void CleanupPlayback()
        {
            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }
            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CleanupPlayback();
        }
    }
}
