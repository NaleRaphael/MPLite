using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace MPLite
{
    using PlaybackMode = Core.PlaybackMode;
    using AppSettings = Core.AppSettings;
    using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

    public partial class PageSetting : Page
    {
        private Key sysKey;
        private Key normalKey;

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
                cmbPlaybackMode.Items.Add(mode);
            }
            cmbPlaybackMode.SelectedIndex = Properties.Settings.Default.PlaybackMode;

            // textbox
            txtPlaylistStoragePath.Text = Path.GetFullPath(AppSettings.TrackDBPath);
            txtSchedulerEventStoragePath.Text = Path.GetFullPath(AppSettings.EventDBPath);
        }

        private void cmbPlaybackMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedMode = cmbPlaybackMode.SelectedItem.ToString();
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

        private void btnSelectSchedulerEventStoragePath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Path.GetFullPath(AppSettings.EventDBPath));
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AppSettings.SetEventDBPath(fbd.SelectedPath);
                txtSchedulerEventStoragePath.Text = AppSettings.EventDBPath;
            }
        }

        private void txtHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Capital:
                case Key.NumLock:
                    e.Handled = true;
                    return;
                default:
                    txtHotkey_KeyDown(sender, e);
                    break;
            }
        }

        private void txtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e == null) return;
            if (e.IsRepeat) return;

            GetSystemKeyAndNormalKey(e, out sysKey, out normalKey, sysKey, normalKey);
            Console.WriteLine(Keyboard.Modifiers);
            Console.WriteLine(string.Format("{0}, {1}, {2}, {3}", e.SystemKey, e.Key, sysKey, normalKey));
            txtHotkey.Text = (sysKey == Key.None) ? normalKey.ToString() : sysKey.ToString() + ((normalKey == Key.None) ? "" : "+" + normalKey.ToString());
        }

        private void GetSystemKeyAndNormalKey(KeyEventArgs e, out Key sKey, out Key nKey, Key sKeyDefault = Key.None, Key nKeyDefault = Key.None)
        {
            bool isModifierKeyPressed = Keyboard.Modifiers != ModifierKeys.None;
            sKey = isModifierKeyPressed ? sKeyDefault : Key.None;
            nKey = isModifierKeyPressed ? nKeyDefault : Key.None;

            switch (e.SystemKey)
            {
                case Key.None:
                    if (Keyboard.Modifiers != ModifierKeys.None)
                    {
                        switch (e.Key)
                        {
                            case Key.LeftCtrl:
                            case Key.LeftShift:
                            case Key.RightCtrl:
                            case Key.RightShift:
                                sKey = e.Key;
                                break;
                            default:
                                nKey = e.Key;
                                break;
                        }
                    }
                    else
                    {
                        nKey = e.Key;
                    }
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    sKey = e.SystemKey;
                    break;
                default:
                    nKey = e.SystemKey;
                    break;
            }
        }
    }
}
