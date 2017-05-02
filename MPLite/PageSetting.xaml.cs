using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MPLite
{
    using PlaybackMode = Core.PlaybackMode;
    using AppSettings = Core.AppSettings;
    using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

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
            txtPlaylistStoragePath.Text = Path.GetFullPath(AppSettings.TrackDBPath);
            txtCalendarEventStoragePath.Text = Path.GetFullPath(AppSettings.EventDBPath);
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
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Path.GetFullPath(AppSettings.TrackDBPath));
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(Path.GetDirectoryName(fbd.SelectedPath)))
                {
                    AppSettings.SetTrackDBPath(fbd.SelectedPath);
                    txtPlaylistStoragePath.Text = AppSettings.TrackDBPath;
                }
            }
        }

        private void btnSelectCalendarEventStoragePath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Path.GetFullPath(AppSettings.EventDBPath));
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AppSettings.SetEventDBPath(fbd.SelectedPath);
                txtCalendarEventStoragePath.Text = AppSettings.EventDBPath;
            }
        }
    }
}
