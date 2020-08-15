using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class SeriesItem : ExtendedBaseItem
    {
        public string Overview { get; set; }

        public string OfficialRating { get; set; }        

        public double? CommunityRating { get; set; }

        public int? ProductionYear { get; set; }

        public string[] Genres { get; set; } = new string[0];

        public ExternalUrl[] ExternalUrls { get; set; } = new ExternalUrl[0];

        public Person[] People { get; set; }

    }
}
