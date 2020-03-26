using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Jellyfin.ApiClient
{
    public class JellyfinMessageProcessingHandler : MessageProcessingHandler
    {
        public JellyfinMessageProcessingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        { }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            return response;
        }
    }
}
