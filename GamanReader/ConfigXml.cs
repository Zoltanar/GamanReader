using System;
using System.Collections.Generic;

namespace GamanReader
{
    [Serializable]
    public class ConfigXml
    {
        // ReSharper disable once EmptyConstructor
        public ConfigXml()
        {
            RecentListSize = 25;
            RecentFolders = new List<string>();
        }
        public ConfigXml(List<string> recentFolders, int recentListSize)
        {
            RecentFolders = recentFolders;
            RecentListSize = recentListSize < 1 ? 25 : recentListSize;
        }
        public List<string> RecentFolders { get; set; }
        public int RecentListSize { get; set; }
    }
}
