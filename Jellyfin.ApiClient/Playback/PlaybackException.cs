using MediaBrowser.Model.Dlna;
using System;

namespace Jellyfin.ApiClient.Playback
{
    public class PlaybackException : Exception
    {
        public PlaybackErrorCode ErrorCode { get; set; }
    }
}
