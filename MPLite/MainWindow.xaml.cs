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
        private bool isWindowMaximized = false;

        // Pages
        private PagePlaylist pagePlaylist = null;
        private PageSetting pageSetting = null;
        private PageCalendar pageCalendar = null;

        // Menu_Setting
        private bool isMenuCollapsed = true;

        // Try to turn off navigation sound
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

            // Page switcher
            PageSwitcher.pageSwitcher = this.Frame_PageSwitcher;

            // Menu_Setting
            Menu_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;

            // Default page
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);
        }

        #region PageControl
        private void PageSwitchControl<T>(ref T target) where T : Page, new()
        {
            if (target == null)
            {
                target = new T();
            }
            PageSwitcher.Switch(target);
        }

        private void Btn_Playlist_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PagePlaylist>(ref pagePlaylist);
        }

        private void Btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            //isMenuCollapsed = !isMenuCollapsed;
            //Menu_Setting.Visibility = isMenuCollapsed ? Visibility.Collapsed : Visibility.Visible;
            CollapseMenuSetting(false);
        }

        private void MItem_Basic_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate to desired page
            PageSwitchControl<PageSetting>(ref pageSetting);
            CollapseMenuSetting(true);
        }

        private void MItem_Scheduler_Click(object sender, RoutedEventArgs e)
        {
            PageSwitchControl<PageCalendar>(ref pageCalendar);
            CollapseMenuSetting(true);
            /*if (pageScheduler == null)
            {
                pageScheduler = new PageCalendar();
            }
            else
            {
                return;
            }
            Frame_PageSwitcher.NavigationService.Navigate(pageScheduler);*/

            /*if (proxyScheduler == null)
            {
                proxyScheduler = new ProxyWindow();
            }*/
        }
        #endregion

        private void DPane_Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
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

        private void CollapseMenuSetting(bool collapse)
        {
            Menu_Setting.Visibility = collapse ? Visibility.Collapsed : Visibility.Visible;
            isMenuCollapsed = collapse;
        }

        private void CloseProxyWindow()
        {
            //scheduler;
        }

        private void Btn_StartPlayback_Click(object sender, RoutedEventArgs e)
        {
            // TODO: play music
            // TODO: show status of playback
        }

        
    }
}
