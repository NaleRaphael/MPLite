using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace MPLite
{
    public partial class MainWindow : Window
    {
        // Window control
        bool isWindowMaximized = false;

        private const int Feature = 21; //FEATURE_DISABLE_NAVIGATION_SOUNDS
        private const int SetFeatureOnProcess = 0x00000002;
        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(int featureEntry,
            [MarshalAs(UnmanagedType.U4)] int dwFlags, bool fEnable);

        public MainWindow()
        {
            InitializeComponent();
            //CoInternetSetFeatureEnabled(Feature, SetFeatureOnProcess, true);
            URLSecurityZoneAPI.InternetSetFeatureEnabled(URLSecurityZoneAPI.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, URLSecurityZoneAPI.SetFeatureOn.PROCESS, true);
        }

        private void DPane_Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Btn_Playlist_Click(object sender, RoutedEventArgs e)
        {
            URLSecurityZoneAPI.InternetSetFeatureEnabled(URLSecurityZoneAPI.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, URLSecurityZoneAPI.SetFeatureOn.PROCESS, true);
            Frame_PageSwitcher.NavigationService.Navigate(new PagePlaylist());
        }

        private void Btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            URLSecurityZoneAPI.InternetSetFeatureEnabled(URLSecurityZoneAPI.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, URLSecurityZoneAPI.SetFeatureOn.PROCESS, true);
            Frame_PageSwitcher.NavigationService.Navigate(new PageSetting());
        }

        private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isWindowMaximized)
            {
                isWindowMaximized = !isWindowMaximized;
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
            else {
                isWindowMaximized = false;
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        private void Btn_ExitProgram_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Frame_PageSwitcher.NavigationService.Navigate(new PageCalendar());
        }
    }
}
