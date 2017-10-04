using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Security;
using System.Runtime.InteropServices;

namespace MPLite
{
    using PlaybackMode = Core.PlaybackMode;
    using TrackStatus = Core.TrackStatus;

    public static class MPLiteSetting
    {
        private static Hotkeys _hotkeys = null;
        private static List<string> _hotkeyNames = new List<string> { "Play & Pause", "Stop", "Forward", "Backward",
            "Increase Volume", "Decrease Volume", "Fast Forward", "Reverse" };

        public static int Volume
        {
            set
            {
                if (value > 1000 || value < 0)
                    throw new Exception("Value of volume shoud be in the range between 0 - 1000.");

                Properties.Settings.Default.Volume = value;
                Properties.Settings.Default.Save();
            }
            get
            {
                return Properties.Settings.Default.IsMuted ? 0 : Properties.Settings.Default.Volume;
            }
        }

        public static bool IsMuted
        {
            set
            {
                Properties.Settings.Default.IsMuted = value;
                Properties.Settings.Default.Save();
            }
            get
            {
                return Properties.Settings.Default.IsMuted;
            }
        }

        public static string HotkeysSettingPath
        {
            set
            {
                Properties.Settings.Default.HotkeysSettingPath = value;
                Properties.Settings.Default.Save();
            }
            get
            {
                return Properties.Settings.Default.HotkeysSettingPath;
            }
        }

        public static Hotkeys Hotkeys
        {
            get
            {
                if (_hotkeys == null)
                    _hotkeys = Hotkeys.Load();
                return _hotkeys;
            }
        }

        public static List<string> HotkeyNames
        {
            get { return _hotkeyNames; }
        }

        public static bool IsEditing { get; set; }

        public static bool IsLaunchAtStartup
        {
            get { return Properties.Settings.Default.IsLaunchAtStartup; }
            set
            {
                Properties.Settings.Default.IsLaunchAtStartup = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool MinimizeWhenExiting
        {
            get { return Properties.Settings.Default.MinimizeWhenExiting; }
            set
            {
                Properties.Settings.Default.MinimizeWhenExiting = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool KeepPlayingAfterCatchingError
        {
            get { return Properties.Settings.Default.KeepPlayingAfterCatchingError; }
            set
            {
                Properties.Settings.Default.KeepPlayingAfterCatchingError = value;
                Properties.Settings.Default.Save();
            }
        }
    }

    public static class MPLiteExtension
    {
        internal static void SetAppProperties(this Properties.Settings setting, Guid listGUID, int trackIdx, TrackStatus status, PlaybackMode mode)
        {
            setting.TaskPlaylistGUID = listGUID;
            setting.TaskPlayingTrackIndex = trackIdx;
            setting.TaskPlayingTrackStatus = (int)status;
            setting.TaskPlaybackMode = (int)mode;
            setting.Save();
        }

        internal static void ResetAppProperties(this Properties.Settings setting)
        {
            setting.TaskPlaylistGUID = Guid.Empty;
            setting.TaskPlayingTrackIndex = -1;
            setting.TaskPlayingTrackStatus = 0;
            setting.TaskPlaybackMode = 0;
            setting.Save();
        }
    }

    public static class ListViewExtension
    {
        internal static List<int> GetSelectedIndices(this ListView lv)
        {
            List<int> selectedIndices = new List<int>(lv.SelectedItems.Count);
            if (lv.SelectedItems.Count == 0)
                return selectedIndices;

            for (int i = 0; i < lv.Items.Count; i++)
            {
                ListViewItem lvi = lv.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

                // Workaround: avoid NullReferenceException being thrown
                if (lvi == null) continue;

                if (lvi.IsSelected)
                    selectedIndices.Add(i);
            }
            return selectedIndices;
        }
    }

    public static class ObservableCollectionExtension
    {
        internal static void FillContent<T>(this ObservableCollection<T> oc, List<T> source)
        {
            if (source == null) return;

            oc.Clear();
            foreach (T obj in source)
                oc.Add(obj);
        }
    }

    /// <summary>
    /// Enables or disables a specified Internet Explorer feature control
    /// Minimum availability: Internet Explorer 6.0
    /// Minimum operating systems: Windows XP SP2
    /// </summary>
    internal class URLSecurityZoneAPI
    {
        /// <summary>
        /// Specifies where to set the feature control value
        /// http://msdn.microsoft.com/en-us/library/ms537168%28VS.85%29.aspx
        /// </summary>
        public enum SetFeatureOn : int
        {
            THREAD = 0x00000001,
            PROCESS = 0x00000002,
            REGISTRY = 0x00000004,
            THREAD_LOCALMACHINE = 0x00000008,
            THREAD_INTRANET = 0x00000010,
            THREAD_TRUSTED = 0x00000020,
            THREAD_INTERNET = 0x00000040,
            THREAD_RESTRICTED = 0x00000080
        }

        /// <summary>
        /// InternetFeaturelist
        /// http://msdn.microsoft.com/en-us/library/ms537169%28v=VS.85%29.aspx
        /// </summary>
        public enum InternetFeaturelist : int
        {
            OBJECT_CACHING = 0,
            ZONE_ELEVATION = 1,
            MIME_HANDLING = 2,
            MIME_SNIFFING = 3,
            WINDOW_RESTRICTIONS = 4,
            WEBOC_POPUPMANAGEMENT = 5,
            BEHAVIORS = 6,
            DISABLE_MK_PROTOCOL = 7,
            LOCALMACHINE_LOCKDOWN = 8,
            SECURITYBAND = 9,
            RESTRICT_ACTIVEXINSTALL = 10,
            VALIDATE_NAVIGATE_URL = 11,
            RESTRICT_FILEDOWNLOAD = 12,
            ADDON_MANAGEMENT = 13,
            PROTOCOL_LOCKDOWN = 14,
            HTTP_USERNAME_PASSWORD_DISABLE = 15,
            SAFE_BINDTOOBJECT = 16,
            UNC_SAVEDFILECHECK = 17,
            GET_URL_DOM_FILEPATH_UNENCODED = 18,
            TABBED_BROWSING = 19,
            SSLUX = 20,
            DISABLE_NAVIGATION_SOUNDS = 21,
            DISABLE_LEGACY_COMPRESSION = 22,
            FORCE_ADDR_AND_STATUS = 23,
            XMLHTTP = 24,
            DISABLE_TELNET_PROTOCOL = 25,
            FEEDS = 26,
            BLOCK_INPUT_PROMPTS = 27,
            MAX = 28
        }

        /// <summary>
        /// Enables or disables a specified feature control. 
        /// http://msdn.microsoft.com/en-us/library/ms537168%28VS.85%29.aspx
        /// </summary>            
        [DllImport("urlmon.dll", ExactSpelling = true), PreserveSig, SecurityCritical, SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(int featureEntry, [MarshalAs(UnmanagedType.U4)] int dwFlags, bool fEnable);

        /// <summary>
        /// Determines whether the specified feature control is enabled. 
        /// http://msdn.microsoft.com/en-us/library/ms537164%28v=VS.85%29.aspx
        /// </summary>
        [DllImport("urlmon.dll", ExactSpelling = true), PreserveSig, SecurityCritical, SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetIsFeatureEnabled(int featureEntry, int dwFlags);

        /// <summary>
        /// Set the internet feature enabled/disabled
        /// </summary>
        /// <param name="feature">The feature from <c>InternetFeaturelist</c></param>
        /// <param name="target">The target from <c>SetFeatureOn</c></param>
        /// <param name="enabled">enabled the feature?</param>
        /// <returns><c>true</c> if [is internet set feature enabled] [the specified feature]; otherwise, <c>false</c>.</returns>
        public static bool InternetSetFeatureEnabled(InternetFeaturelist feature, SetFeatureOn target, bool enabled)
        {
            return (CoInternetSetFeatureEnabled((int)feature, (int)target, enabled) == 0);
        }

        /// <summary>
        /// Determines whether the internet feature is enabled.
        /// </summary>
        /// <param name="feature">The feature from <c>InternetFeaturelist</c></param>
        /// <param name="target">The target from <c>SetFeatureOn</c></param>
        /// <returns><c>true</c> if the internet feature is enabled; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInternetSetFeatureEnabled(InternetFeaturelist feature, SetFeatureOn target)
        {
            return (CoInternetIsFeatureEnabled((int)feature, (int)target) == 0);
        }

    }
}
