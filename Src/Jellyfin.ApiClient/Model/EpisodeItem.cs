﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class EpisodeItem : BaseItem
    {
        public int IndexNumber { get; set; }
        public int ParentIndexNumber { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public String Overview { get; set; }

        public String SeriesId { get; set; }
        public String SeasonId { get; set; }
        public String SeriesPrimaryImageTag { get; set; }
        public String[] ParentBackdropImageTags { get; set; }
    }
}
