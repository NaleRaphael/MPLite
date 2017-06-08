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
        private ModifierKeys sysKey;
        private Key normalKey;
        public static Hotkeys MPLiteHotKeys = null;

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

            // hotkey
            MPLiteHotKeys = Hotkeys.Load();
            cmbHotkey.ItemsSource = MPLiteHotKeys;
            cmbHotkey.SelectedIndex = 0;

            // chkLaunchSetting
            chkLaunchSetting.IsChecked = MPLiteSetting.IsLaunchAtStartup;
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

            Hotkey.GetSystemKeyAndNormalKey(e, Keyboard.Modifiers, out sysKey, out normalKey, sysKey, normalKey);
#if DEBUG
            Console.WriteLine(DateTime.Now);
            Console.WriteLine(Keyboard.Modifiers);
            Console.WriteLine(string.Format("{0}, {1}, {2}, {3}", e.SystemKey, e.Key, sysKey, normalKey));
#endif
            txtHotkey.Text = (sysKey == ModifierKeys.None) ? normalKey.ToString() : sysKey.ToString() + ((normalKey == Key.None) ? "" : "+" + normalKey.ToString());
            txtHotkey.CaretIndex = txtHotkey.Text.Length;
        }

        private void txtHotkey_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MPLiteSetting.IsEditing = true;
        }

        private void txtHotkey_LostFocus(object sender, RoutedEventArgs e)
        {
            MPLiteSetting.IsEditing = false;
        }

        private void btnSaveHotkey_Click(object sender, RoutedEventArgs e)
        {
            txtHotkey.IsReadOnly = true;
            if (txtHotkey.Text == "") return;

            Hotkey hotkey = cmbHotkey.SelectedItem as Hotkey;
            if (hotkey == null) return;

            if (hotkey.TrySet(txtHotkey.Text, '+'))
            {
                MessageBox.Show("Sucess");
            }

            MPLiteHotKeys.Save();
        }

        private void cmbHotkey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Hotkey hotkey = cmbHotkey.SelectedItem as Hotkey;
            if (hotkey == null) return;

            txtHotkey.Text = ((hotkey.SystemKey == ModifierKeys.None) ? "" : hotkey.SystemKey.ToString() + "+") 
                + ((hotkey.NormalKey == Key.None) ? "" : hotkey.NormalKey.ToString());

            txtHotkey.CaretIndex = txtHotkey.Text.Length;
        }

        private void chkLaunchSetting_Click(object sender, RoutedEventArgs e)
        {
            if (chkLaunchSetting.IsChecked.Value == MPLiteSetting.IsLaunchAtStartup) return;

            MPLiteSetting.IsLaunchAtStartup = (chkLaunchSetting.IsChecked == true);

            RegistryKey register = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string exePath = "\"" + Path.Combine(baseDir, "MPLite.exe") + "\"";

            if (chkLaunchSetting.IsChecked == true)
                register.SetValue(appName, exePath);
            else
                register.DeleteValue(appName, false);
        }
    }
}
