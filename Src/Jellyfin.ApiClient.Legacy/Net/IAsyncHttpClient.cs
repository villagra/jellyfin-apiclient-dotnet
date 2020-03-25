using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Net
{
    /// <summary>
    /// Interface IHttpClient
    /// </summary>
    public interface IAsyncHttpClient : IDisposable
    {
        /// <summary>
        /// Occurs when [HTTP response received].
        /// </summary>
        event EventHandler<HttpWebResponse> HttpResponseReceived;

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task&lt;Stream&gt;.</returns>
        Task<Stream> SendAsync(HttpRequest options);

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="sendFailureResponse">if set to <c>true</c> [send failure response].</param>
        /// <returns>
        /// Task&lt;HttpResponse&gt;.
        /// </returns>
        Task<HttpWebResponse> GetResponse(HttpRequest options, bool sendFailureResponse = false);
    }
}
