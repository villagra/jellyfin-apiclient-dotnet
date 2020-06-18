using Polly;
using Polly.Extensions.Http;
using Polly.Utilities;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.ApiClient
{
    public class JellyfinHttpClientHandler : HttpClientHandler
    {
        readonly ILogger Logger;

        public JellyfinHttpClientHandler(ILogger logger)
        {
            Logger = logger;

            if (this.SupportsAutomaticDecompression)
            {
                this.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var policy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                            //.Or<TimeoutRejectedException>() // TimeoutRejectedException from Polly's TimeoutPolicy
                            .WaitAndRetryAsync(
                                3,
                                retryAttempt => TimeSpan.FromMilliseconds(retryAttempt*50),
                                onRetry: (response, calculatedWaitDuration) =>
                                {
                                    Logger.LogError($"Failed attempt. Waited for {calculatedWaitDuration}. Retrying. {response.Exception?.Message} - {response.Exception?.StackTrace}");
                                }
                            );

            return policy.ExecuteAsync(async () => 
                {
                    return await base.SendAsync(request, cancellationToken);
                }
            );
        }
    }
}
