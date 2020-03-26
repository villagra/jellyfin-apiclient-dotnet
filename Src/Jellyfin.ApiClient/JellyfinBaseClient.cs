using Jellyfin.ApiClient.Auth;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Jellyfin.ApiClient
{
    public abstract class JellyfinBaseClient
    {
        private readonly IAuthenticationMethod Authentication;

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
            HttpClientHandler httpHandler = new HttpClientHandler();
            if (httpHandler.SupportsAutomaticDecompression)
            {
                httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            var handler = new JellyfinMessageProcessingHandler(httpHandler as HttpMessageHandler);
            Client = new HttpClient(handler)
            {
                BaseAddress = server
            };
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
