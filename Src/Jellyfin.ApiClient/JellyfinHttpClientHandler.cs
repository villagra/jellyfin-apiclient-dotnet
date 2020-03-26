using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient
{
    public class JellyfinHttpClientHandler : HttpClientHandler
    {
        public JellyfinHttpClientHandler()
        {
            if (this.SupportsAutomaticDecompression)
            {
                this.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var policy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                          .Or<TimeoutRejectedException>() // TimeoutRejectedException from Polly's TimeoutPolicy
                          .RetryAsync(3);

            return policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }
    }
}
