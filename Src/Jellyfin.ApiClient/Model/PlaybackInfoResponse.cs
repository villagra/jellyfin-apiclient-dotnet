using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class PlaybackInfoResponse
    {
        public MediaSourceInfo[] MediaSources { get; set; }
        public string PlaySessionId { get; set; }
        public PlaybackErrorCode? ErrorCode { get; set; }
    }
}
