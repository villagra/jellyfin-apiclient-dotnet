using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient
{
    /// <summary>
    /// https://github.com/MediaBrowser/Emby/wiki
    /// </summary>
    public class JellyfinClient : JellyfinBaseClient, IApiClient
    {        

        public JellyfinClient(Uri serverAddress, IAuthenticationMethod authentication)
            : base (serverAddress, authentication, NullLogger.Instance)
        { }

        public JellyfinClient(Uri serverAddress, IAuthenticationMethod authentication, ILogger logger)
            : base(serverAddress, authentication, logger)
        { }

        public Task<AuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
            return null;            
        }

    }
}
