using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MPLite
{
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
            foreach (string mode in Enum.GetNames(typeof(MPLiteConstant.PlaybackMode)))
            {
                if (mode == "None")
                    continue;
                cmb_PlaybackSetting.Items.Add(mode);
            }
            cmb_PlaybackSetting.SelectedIndex = Properties.Settings.Default.PlaybackMode;

            // txtPlaylistStoragePath
            txtPlaylistStoragePath.Text = Path.GetFullPath(Properties.Settings.Default.PlaylistInfoPath);
        }

        private void cmb_PlaybackSetting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedMode = cmb_PlaybackSetting.SelectedItem.ToString();
            MPLiteConstant.PlaybackMode mode;

            if (Enum.TryParse<MPLiteConstant.PlaybackMode>(selectedMode, out mode))
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
        }

        private void btnSelectCalenderEventStoragePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath());
        }
    }
}
