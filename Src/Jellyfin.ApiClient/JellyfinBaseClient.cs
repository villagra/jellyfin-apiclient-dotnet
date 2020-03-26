using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Exceptions;
using Jellyfin.ApiClient.Serialization;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient
{
    public abstract class JellyfinBaseClient
    {
        private readonly JsonSerializer _serializer = new JsonSerializer();
        
        protected IAuthenticationMethod Authentication { get; private set; }       
        protected ILogger Logger { get; private set; }
        protected HttpClient Client { get; private set; }
        protected Uri ServerAddress { get; private set; }
        protected UserDto CurrentUser { get; private set; }
        protected String AccessToken { get; private set; }

        public JellyfinBaseClient(Uri serverAddress, IAuthenticationMethod authentication, ILogger logger)
        {
            this.ServerAddress = serverAddress;
            this.Authentication = authentication;
            this.Logger = logger;

            CreateClient(serverAddress);
        }

        protected void CreateClient(Uri server)
        {            
            var handler = new JellyfinHttpClientHandler(Logger);
            Client = new HttpClient(handler)
            {
                BaseAddress = server
            };

            UpdateHeaders();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }        

        protected void SetAuthenticationInfo(UserDto currentUser, String accessToken)
        {
            this.CurrentUser = currentUser;
            this.AccessToken = accessToken;
            UpdateHeaders();
        }

        private void UpdateHeaders()
        {
            Client.DefaultRequestHeaders.Remove("X-MediaBrowser-Token");

            if (Authentication is UserAuthentication ua)
            {                
                if (!String.IsNullOrWhiteSpace(AccessToken))
                {
                    Client.DefaultRequestHeaders.Add("X-MediaBrowser-Token", AccessToken);
                }

                if (CurrentUser != null)
                {
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MediaBrowser", $"UserId=\"{CurrentUser.Id.ToString()}\", Client=\"{ua.ClientName}\", DeviceId=\"{ua.DeviceId}\", Device=\"{ua.DeviceName}\", Version=\"{ua.ApplicationVersion}\"");
                }
                else
                {
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MediaBrowser", $"Client=\"{ua.ClientName}\", DeviceId=\"{ua.DeviceId}\", Device=\"{ua.DeviceName}\", Version=\"{ua.ApplicationVersion}\"");
                }
            }
        }

        protected async Task<T> DoPost<T>(String path, Object data) where T : class
        {
            HttpResponseMessage response = await Client.PostAsync(path, new JsonContent(data)).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                using (var json = new JsonTextReader(reader))
                {
                    return _serializer.Deserialize<T>(json);
                }
            }

            var stringcontent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new RequestFailedException(response.StatusCode, stringcontent);
        }
    }
}
