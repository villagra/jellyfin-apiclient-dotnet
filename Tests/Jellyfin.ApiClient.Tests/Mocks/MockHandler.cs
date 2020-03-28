using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Tests.Mocks
{
    public abstract class MockHandler : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SendAsync(request.Method, request.RequestUri.PathAndQuery));
        }

        public abstract HttpResponseMessage SendAsync(HttpMethod method, string url);
    }
}
