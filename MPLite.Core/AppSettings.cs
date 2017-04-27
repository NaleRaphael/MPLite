using System;
using System.IO;

namespace MPLite.Core
{
    public static class AppSettings
    {
        public static string PlaylistDatabase
        {
            get
            {
                return Properties.Settings.Default.PlaylistInfoPath;
            }
            set
            {
                if (Path.GetExtension(value) != ".json")
                    throw new Exception("Invalid file extension. It should be a json file.");
                if (!Directory.Exists(Path.GetDirectoryName(value)))
                    throw new Exception("Directory does not exist.");
                Properties.Settings.Default.PlaylistInfoPath = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string EventDatabase
        {
            get
            {
                return Properties.Settings.Default.DBPath;
            }
            set
            {
                if (Path.GetExtension(value) != ".json")
                    throw new Exception("Invalid file extension. It should be a json file.");
                if (!Directory.Exists(Path.GetDirectoryName(value)))
                    throw new Exception("Directory does not exist.");
                Properties.Settings.Default.DBPath = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
