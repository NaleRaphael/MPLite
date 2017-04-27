using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MPLite
{
    using PlaybackMode = Core.PlaybackMode;
    using AppSettings = Core.AppSettings;

    public partial class PageSetting : Page
    {
        public PageSetting()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // cmb_PlaybackSetting
            foreach (string mode in Enum.GetNames(typeof(PlaybackMode)))
            {
                if (mode == "None")
                    continue;
                cmb_PlaybackSetting.Items.Add(mode);
            }
            cmb_PlaybackSetting.SelectedIndex = Properties.Settings.Default.PlaybackMode;

            // textbox
            txtPlaylistStoragePath.Text = Path.GetFullPath(AppSettings.PlaylistDatabase);
            txtCalendarEventStoragePath.Text = Path.GetFullPath(AppSettings.EventDatabase);
        }

        private void cmb_PlaybackSetting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedMode = cmb_PlaybackSetting.SelectedItem.ToString();
            PlaybackMode mode;

            if (Enum.TryParse<PlaybackMode>(selectedMode, out mode))
            {
                if ((int)mode != Properties.Settings.Default.PlaybackMode)
                {
                    Properties.Settings.Default.PlaybackMode = (int)mode;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void btnSelectPlaylistStoragePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Properties.Settings.Default.PlaylistInfoPath));
            if (ofd.ShowDialog() == true)
            {
                txtPlaylistStoragePath.Text = ofd.FileName;
            }

            // save config
        }

        private void btnSelectCalendarEventStoragePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath());
            
        }
    }
}
