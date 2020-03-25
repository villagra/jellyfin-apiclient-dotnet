using Jellyfin.ApiClient.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Jellyfin.ApiClient.WebSocket
{
    /// <summary>
    /// Class WebSocket4NetClientWebSocket
    /// </summary>
    public class WebSocket4NetClientWebSocket : IClientWebSocket
    {
        private readonly ILogger _logger;

        /// <summary>
        /// The _socket
        /// </summary>
        private WebSocket4Net.WebSocket _socket;

        public WebSocket4NetClientWebSocket(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State
        {
            get
            {

                switch (_socket.State)
                {
                    case WebSocket4Net.WebSocketState.Closed:
                        return WebSocketState.Closed;
                    case WebSocket4Net.WebSocketState.Closing:
                        return WebSocketState.Closed;
                    case WebSocket4Net.WebSocketState.Connecting:
                        return WebSocketState.Connecting;
                    case WebSocket4Net.WebSocketState.None:
                        return WebSocketState.None;
                    case WebSocket4Net.WebSocketState.Open:
                        return WebSocketState.Open;
                    default:
                        return WebSocketState.None;
                }
            }
        }

        /// <summary>
        /// Connects the async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task ConnectAsync(Uri url, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                _socket = new WebSocket4Net.WebSocket(url.ToString());

                _socket.MessageReceived += Websocket_MessageReceived;

                _socket.Open();

                _socket.Opened += (sender, args) => taskCompletionSource.TrySetResult(true);
                _socket.Closed += Socket_Closed;
            }
            catch (Exception ex)
            {
                _socket = null;

                taskCompletionSource.TrySetException(ex);
            }

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Handles the WebSocketClosed event of the _socket control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void Socket_Closed(object sender, EventArgs e)
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the MessageReceived event of the websocket control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MessageReceivedEventArgs" /> instance containing the event data.</param>
        void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            OnReceive?.Invoke(e.Message);
        }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Action<byte[]> OnReceiveBytes { get; set; }

        /// <summary>
        /// Gets or sets the on receive.
        /// </summary>
        /// <value>The on receive.</value>
        public Action<string> OnReceive { get; set; }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="type">The type.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] bytes, WebSocketMessageType type, bool endOfMessage, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => _socket.Send(bytes, 0, bytes.Length), cancellationToken);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_socket != null)
            {
                var state = State;

                if (state == WebSocketState.Open || state == WebSocketState.Connecting)
                {
                    _logger.LogInformation("Sending web socket close message");

                    _socket.Close();
                }

                _socket = null;
            }
        }

        /// <summary>
        /// Occurs when [closed].
        /// </summary>
        public event EventHandler Closed;
    }
}
