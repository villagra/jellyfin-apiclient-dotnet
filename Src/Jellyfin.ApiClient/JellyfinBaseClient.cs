using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Exceptions;
using Jellyfin.ApiClient.Serialization;
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
        private readonly IAuthenticationMethod Authentication;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        protected ILogger Logger { get; private set; }
        protected HttpClient Client { get; private set; }
        protected Uri ServerAddress { get; private set; }

        public JellyfinBaseClient(Uri serverAddress, IAuthenticationMethod authentication, ILogger logger)
        {
            this.ServerAddress = serverAddress;
            this.Authentication = authentication;
            this.Logger = logger;

            CreateClient(serverAddress);
        }

        protected void CreateClient(Uri server)
        {            
            var handler = new JellyfinHttpClientHandler();
            Client = new HttpClient(handler)
            {
                BaseAddress = server
            };

            if (Authentication is UserAuthentication ua)
            {
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MediaBrowser", $"Client=\"{ua.ClientName}\", DeviceId=\"{ua.DeviceId}\", Device=\"{ua.DeviceName}\", Version=\"{ua.ApplicationVersion}\"");
            }

            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
