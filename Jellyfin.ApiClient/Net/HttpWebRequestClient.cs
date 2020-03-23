using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Net
{
    /// <summary>
    /// Class HttpWebRequestClient
    /// </summary>
    public class HttpWebRequestClient : IAsyncHttpClient
    {
        public event EventHandler<HttpWebResponse> HttpResponseReceived;
        private readonly IHttpWebRequestFactory _requestFactory;

        /// <summary>
        /// Called when [response received].
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="requestTime">The request time.</param>
        private void OnResponseReceived(Uri url, 
            string verb, 
            HttpWebResponse response,
            DateTime requestTime)
        {
            var duration = DateTime.Now - requestTime;

            Logger.LogDebug("Received {0} status code after {1} ms from {2}: {3}", (int)response.StatusCode, duration.TotalMilliseconds, verb, url);

            HttpResponseReceived?.Invoke(this, response);
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="requestFactory">The request factory.</param>
        public HttpWebRequestClient(ILogger logger, IHttpWebRequestFactory requestFactory)
        {
            Logger = logger;
            _requestFactory = requestFactory;
        }

        public async Task<HttpWebResponse> GetResponse(HttpRequest options, bool sendFailureResponse = false)
        {
            options.CancellationToken.ThrowIfCancellationRequested();

            var httpWebRequest = _requestFactory.Create(options);

            ApplyHeaders(options.RequestHeaders, httpWebRequest);

            if (options.RequestStream != null)
            {
                httpWebRequest.ContentType = options.RequestContentType;
                _requestFactory.SetContentLength(httpWebRequest, options.RequestStream.Length);

                using (var requestStream = await _requestFactory.GetRequestStreamAsync(httpWebRequest).ConfigureAwait(false))
                {
                    await options.RequestStream.CopyToAsync(requestStream).ConfigureAwait(false);
                }
            }
            else if (!string.IsNullOrEmpty(options.RequestContent) ||
                string.Equals(options.Method, "post", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = Encoding.UTF8.GetBytes(options.RequestContent ?? string.Empty);

                httpWebRequest.ContentType = options.RequestContentType ?? "application/x-www-form-urlencoded";
                _requestFactory.SetContentLength(httpWebRequest, bytes.Length);

                using (var requestStream = await _requestFactory.GetRequestStreamAsync(httpWebRequest).ConfigureAwait(false))
                {
                    await requestStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }

            Logger.LogDebug(options.Method + " {0}", options.Url);

            var requestTime = DateTime.Now;

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                var response = await _requestFactory.GetResponseAsync(httpWebRequest, options.Timeout).ConfigureAwait(false);

                var httpResponse = (HttpWebResponse)response;

                OnResponseReceived(options.Url, options.Method, httpResponse, requestTime);

                EnsureSuccessStatusCode(httpResponse);

                options.CancellationToken.ThrowIfCancellationRequested();

                return httpResponse;
            }
            catch (OperationCanceledException ex)
            {
                var exception = GetCancellationException(options.Url, options.CancellationToken, ex);

                throw exception;
            }
            catch (Exception ex)
            {
                if (sendFailureResponse)
                {
                    var webException = ex as WebException ?? ex.InnerException as WebException;
                    if (webException != null)
                    {
                        if (webException.Response is HttpWebResponse response)
                        {
                            return response;
                        }
                    }
                }
                
                throw GetExceptionToThrow(ex, options, requestTime);
            }
        }

        public async Task<Stream> SendAsync(HttpRequest options)
        {
            var response = await GetResponse(options).ConfigureAwait(false);

            return response.GetResponseStream();
        }

        private Exception GetExceptionToThrow(Exception ex, HttpRequest options, DateTime requestTime)
        {
            var webException = ex as WebException ?? ex.InnerException as WebException;

            if (webException != null)
            {
                Logger.LogError("Error getting response from " + options.Url, ex);

                var httpException = new HttpException(ex.Message, ex);

                if (webException.Response is HttpWebResponse response)
                {
                    httpException.StatusCode = response.StatusCode;
                    OnResponseReceived(options.Url, options.Method, response, requestTime);
                }

                return httpException;
            }

            var timeoutException = ex as TimeoutException ?? ex.InnerException as TimeoutException;
            if (timeoutException != null)
            {
                Logger.LogError("Request timeout to " + options.Url, ex);
                
                var httpException = new HttpException(ex.Message, ex)
                {
                    IsTimedOut = true
                };

                return httpException;
            }

            Logger.LogError("Error getting response from " + options.Url, ex);
            return ex;
        }

        private void ApplyHeaders(HttpHeaders headers, HttpWebRequest request)
        {
            foreach (var header in headers)
            {
                request.Headers[header.Key] = header.Value;
            }

            if (!string.IsNullOrEmpty(headers.AuthorizationScheme))
            {
                var val = string.Format("{0} {1}", headers.AuthorizationScheme, headers.AuthorizationParameter);
                request.Headers["X-Emby-Authorization"] = val;
            }
        }

        /// <summary>
        /// Throws the cancellation exception.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>Exception.</returns>
        private Exception GetCancellationException(Uri url, CancellationToken cancellationToken, OperationCanceledException exception)
        {
            // If the HttpClient's timeout is reached, it will cancel the Task internally
            if (!cancellationToken.IsCancellationRequested)
            {
                var msg = string.Format("Connection to {0} timed out", url);

                Logger.LogError(msg);

                // Throw an HttpException so that the caller doesn't think it was cancelled by user code
                return new HttpException(msg, exception)
                {
                    IsTimedOut = true
                };
            }

            return exception;
        }

        private void EnsureSuccessStatusCode(HttpWebResponse response)
        {
            var statusCode = response.StatusCode;
            var isSuccessful = statusCode >= HttpStatusCode.OK && statusCode <= (HttpStatusCode)299;

            if (!isSuccessful)
            {
                throw new HttpException(response.StatusDescription) { StatusCode = response.StatusCode };
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }

    public interface IHttpWebRequestFactory
    {
        HttpWebRequest Create(HttpRequest options);

        void SetContentLength(HttpWebRequest request, long length);

        Task<WebResponse> GetResponseAsync(HttpWebRequest request, int timeoutMs);
        Task<Stream> GetRequestStreamAsync(HttpWebRequest request);
    }

    public static class AsyncHttpClientFactory
    {
        public static IAsyncHttpClient Create(ILogger logger)
        {
#if PORTABLE || WINDOWS_UWP
            return new HttpWebRequestClient(logger, new PortableHttpWebRequestFactory());
#else
            return new HttpWebRequestClient(logger, new HttpWebRequestFactory());
#endif
        }
    }
}
