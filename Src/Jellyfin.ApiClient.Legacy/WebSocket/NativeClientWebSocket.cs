﻿using Jellyfin.ApiClient.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.WebSocket
{
    /// <summary>
    /// Class NativeClientWebSocket
    /// </summary>
    public class NativeClientWebSocket : IClientWebSocket
    {
        /// <summary>
        /// Occurs when [closed].
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// The _client
        /// </summary>
        private readonly ClientWebSocket _client;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _send resource
        /// </summary>
        private readonly SemaphoreSlim _sendResource = new SemaphoreSlim(1, 1);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NativeClientWebSocket" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public NativeClientWebSocket(ILogger logger)
        {
            _logger = logger;
            _client = new ClientWebSocket();
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
        /// Connects the async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ConnectAsync(Uri url, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.ConnectAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error connecting to {0}", ex, url);

                throw;
            }

            Receive();
        }

        /// <summary>
        /// Receives this instance.
        /// </summary>
        private async void Receive()
        {
            while (true)
            {
                byte[] bytes;

                try
                {
                    bytes = await ReceiveBytesAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    OnClosed();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error receiving web socket message", ex);

                    break;
                }

                // Connection closed
                if (bytes == null)
                {
                    break;
                }

                OnReceiveBytes?.Invoke(bytes);
            }
        }

        /// <summary>
        /// Receives the async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{WebSocketMessageInfo}.</returns>
        /// <exception cref="System.Net.WebSockets.WebSocketException">Connection closed</exception>
        private async Task<byte[]> ReceiveBytesAsync(CancellationToken cancellationToken)
        {
            var bytes = new byte[4096];
            var buffer = new ArraySegment<byte>(bytes);

            var result = await _client.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (result.CloseStatus.HasValue)
            {
                OnClosed();
                return null;
            }

            return buffer.Array;
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="type">The type.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task SendAsync(byte[] bytes, WebSocketMessageType type, bool endOfMessage, CancellationToken cancellationToken = default)
        {
            await _sendResource.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {

                if (!Enum.TryParse(type.ToString(), true, out System.Net.WebSockets.WebSocketMessageType nativeType))
                {
                    _logger.LogWarning("Unrecognized WebSocketMessageType: {0}", type.ToString());
                }

                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

                await _client.SendAsync(new ArraySegment<byte>(bytes), nativeType, true, linkedTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                _sendResource.Release();
            }
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State
        {
            get
            {
                if (_client == null)
                {
                    return WebSocketState.None;
                }


                if (!Enum.TryParse(_client.State.ToString(), true, out WebSocketState commonState))
                {
                    _logger.LogWarning("Unrecognized WebSocketState: {0}", _client.State.ToString());
                }

                return commonState;
            }
        }

        /// <summary>
        /// Called when [closed].
        /// </summary>
        void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _cancellationTokenSource.Cancel();
                
                if (_client != null)
                {
                    if (_client.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        _logger.LogInformation("Sending web socket close message.");

                        // Can't wait on this. Try to close gracefully though.
                        _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    _client.Dispose();
                }
            }
        }
    }
}
