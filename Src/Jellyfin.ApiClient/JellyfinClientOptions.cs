using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Jellyfin.ApiClient
{    
    public class JellyfinClientOptions
    {
        public HttpClientHandler Handler { get; set; }
        public ILogger Logger { get; set; }

    }
}
