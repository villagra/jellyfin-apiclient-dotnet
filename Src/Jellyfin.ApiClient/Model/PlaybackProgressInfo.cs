using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class PlaybackProgressInfo
    {
        public long? PositionTicks { get; set; }
        public string MediaSourceId { get; set; }
        public Guid ItemId { get; set; }
    }
}
