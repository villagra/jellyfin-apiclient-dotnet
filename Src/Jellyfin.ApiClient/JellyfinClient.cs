using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Exceptions;
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

            AuthenticationResult result;

            try
            {
                result = await DoPost<AuthenticationResult>("Users/AuthenticateByName", new AuthenticationRequest(username, password));
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == System.Net.HttpStatusCode.InternalServerError || ex.Status == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException();
                }

                throw;
            }

            if (result != null)
            {
                SetAuthenticationInfo(result.User, result.AccessToken);
            }

            return result;         
        }

        public async Task<QueryResult<BaseItem>> GetUserViews(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return await DoGet<QueryResult<BaseItem>>($"Users/{userId}/Views");
        }

    }
}
