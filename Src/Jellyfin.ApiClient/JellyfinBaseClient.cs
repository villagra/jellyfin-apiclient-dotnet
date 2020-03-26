using Jellyfin.ApiClient.Auth;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient
{
    public abstract class JellyfinBaseClient
    {
        private readonly IAuthenticationMethod authentication;
        private readonly ILogger logger;

        public Uri ServerAddress { get; }

        public JellyfinBaseClient(Uri serverAddress, IAuthenticationMethod authentication, ILogger logger)
        {
            ServerAddress = serverAddress;
            this.authentication = authentication;
            this.logger = logger;
        }        
    }
}
