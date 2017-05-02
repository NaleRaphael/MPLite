using System;
using System.IO;

namespace MPLite.Core
{
    public static class AppSettings
    {
        public static string TrackDBPath
        {
            get
            {
                return Properties.Settings.Default.TrackDBPath;
            }
            private set
            {
                // value should be a directory
                if ((File.GetAttributes(value) & FileAttributes.Directory) != FileAttributes.Directory)
                    throw new Exception("Given value should be a directory.");
                if (!Directory.Exists(value))
                    throw new Exception("Directory does not exist.");

                Properties.Settings.Default.TrackDBPath = Path.Combine(value, Properties.Settings.Default.TrackDBName);
                Properties.Settings.Default.Save();
            }
        }

        public static string EventDBPath
        {
            get
            {
                return Properties.Settings.Default.EventDBPath;
            }
            set
            {
                // value should be a directory
                if ((File.GetAttributes(value) & FileAttributes.Directory) != FileAttributes.Directory)
                    throw new Exception("Given value should be a directory.");
                if (!Directory.Exists(value))
                    throw new Exception("Directory does not exist.");

                Properties.Settings.Default.EventDBPath = Path.Combine(value, Properties.Settings.Default.EventDBName);
                Properties.Settings.Default.Save();
            }
        }

        public static void SetTrackDBPath(string directory)
        {
            TrackDBPath = directory;
        }

        public static void SetEventDBPath(string directory)
        {
            EventDBPath = directory;
        }
    }
}
