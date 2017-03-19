using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Itenso.Windows.Controls.ListViewLayout;
using CSCore;
using CSCore.SoundOut;
using CSCore.Codecs.MP3;
using System.Threading;

namespace MPLite
{
    public partial class PagePlaylist : Page
    {
        public PagePlaylist()
        {
            InitializeComponent();
            InitPlaylist();
            ListBox_Playlist.SelectedIndex = 0;  // select default list
        }

        private void InitPlaylist()
        {
            //this.LV_Playlist.ItemsSource;
            PlaylistCollection plc = PlaylistCollection.GetDatabase();
            if (plc == null) return;

            foreach (TrackInfo track in plc.TrackLists[0].Soundtracks)
            {
                LV_Playlist.Items.Add(track);
            }
        }

        private void LV_Playlist_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void LV_Playlist_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string selectedPlaylist = ((ListBoxItem)ListBox_Playlist.SelectedValue).Content.ToString();

            // Update database
            PlaylistCollection.UpdateByTracks(files, selectedPlaylist);

            // Update LV_Playlist
            foreach (string filePath in files)
            {
                // TODO: rewrite this
                //       Store data into a dataset (json file)
                //       Then present those information into listview
                string trackName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                TrackInfo trackInfo = new TrackInfo { TrackName = trackName, TrackPath = filePath };
                LV_Playlist.Items.Add(trackInfo);
            }
        }

        private void LV_Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show(LV_Playlist.SelectedIndex.ToString());
            Stream stream = File.Open(((TrackInfo)LV_Playlist.SelectedItem).TrackPath, FileMode.Open);
            //Contains the sound to play
            using (IWaveSource soundSource = GetSoundSource(stream))
            {
                //SoundOut implementation which plays the sound
                using (ISoundOut soundOut = GetSoundOut())
                {
                    //Tell the SoundOut which sound it has to play
                    soundOut.Initialize(soundSource);
                    //Play the sound
                    soundOut.Play();

                    //Thread.Sleep(2000);

                    //Stop the playback
                    //soundOut.Stop();
                }
            }
            //stream.Close();
        }

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        private IWaveSource GetSoundSource(Stream stream)
        {
            // Instead of using the CodecFactory as helper, you specify the decoder directly:
            return new DmoMp3Decoder(stream);

        }
    }
}
