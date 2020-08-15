using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class MovieItem : ExtendedBaseItem
    {
        public Boolean HasSubtitles { get; set; }        
        public DateTime PremiereDate { get; set; }
        public string OfficialRating { get; set; }
        public double CommunityRating { get; set; }
        public long RunTimeTicks { get; set; }
        public int? ProductionYear { get; set; }
        public string Overview { get; set; }
        public string[] Genres { get; set; }
        public Person[] People { get; set; }
        public MediaStream[] MediaStreams { get; set; }
    }
}
