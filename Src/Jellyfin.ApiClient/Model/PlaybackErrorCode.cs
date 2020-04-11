using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public enum PlaybackErrorCode
    {
        NotAllowed = 0,
        NoCompatibleStream = 1,
        RateLimitExceeded = 2
    }
}
