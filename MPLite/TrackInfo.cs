using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
{
    class TrackInfo
    {
        string TrackName { get; set; }
        string Path { get; set; }

        public TrackInfo(string trackName, string path)
        {
            TrackName = trackName;
            Path = path;
        }
    }
}
