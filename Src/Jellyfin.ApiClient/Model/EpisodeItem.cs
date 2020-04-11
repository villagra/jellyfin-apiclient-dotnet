using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class EpisodeItem : BaseItem
    {
        public int IndexNumber { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public String Overview { get; set; }

        public String SeriesId { get; set; }
        public String SeasonId { get; set; }
    }
}
