﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class MovieItem : BaseItem
    {
        public Boolean HasSubtitles { get; set; }        
        public DateTime PremiereDate { get; set; }
        public string OfficialRating { get; set; }
        public double CommunityRating { get; set; }
        public long RunTimeTicks { get; set; }
        public int ProductionYear { get; set; }                 

    }
}