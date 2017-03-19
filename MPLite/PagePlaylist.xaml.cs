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

namespace MPLite
{
    public partial class PagePlaylist : Page
    {
        public PagePlaylist()
        {
            InitializeComponent();
            ListBox_Playlist.SelectedIndex = 0;
        }

        private void InitPlaylist()
        {
            //this.LV_Playlist.ItemsSource;
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
    }
}
