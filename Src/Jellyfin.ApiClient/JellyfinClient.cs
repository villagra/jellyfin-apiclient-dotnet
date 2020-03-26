using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Model;
using Jellyfin.ApiClient.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
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

        public async Task<AuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            AuthenticationResult result = await DoPost<AuthenticationResult>("Users/AuthenticateByName", new AuthenticationRequest(username, password));          

            //SetAuthenticationInfo(result.AccessToken, result.User.Id);
            //Authenticated?.Invoke(this, new GenericEventArgs<AuthenticationResult>(result));

            return result;         
        }

    }
}
