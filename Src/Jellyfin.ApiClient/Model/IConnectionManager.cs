using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Model
{
    public interface IConnectionManager
    {
        /// <summary>
        /// Occurs when [connected].
        /// </summary>
        event EventHandler<GenericEventArgs<ConnectionResult>> Connected;
        /// <summary>
        /// Occurs when [local user sign in].
        /// </summary>
        event EventHandler<GenericEventArgs<UserDto>> LocalUserSignIn;
        /// <summary>
        /// Occurs when [local user sign out].
        /// </summary>
        event EventHandler<GenericEventArgs<IApiClient>> LocalUserSignOut;
        /// <summary>
        /// Occurs when [remote logged out].
        /// </summary>
        event EventHandler<EventArgs> RemoteLoggedOut;

        /// <summary>
        /// Gets the device.
        /// </summary>
        /// <value>The device.</value>
        IDevice Device { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [save local credentials].
        /// </summary>
        /// <value><c>true</c> if [save local credentials]; otherwise, <c>false</c>.</value>
        bool SaveLocalCredentials { get; set; }

        /// <summary>
        /// Gets the client capabilities.
        /// </summary>
        /// <value>The client capabilities.</value>
        ClientCapabilities ClientCapabilities { get; }

        /// <summary>
        /// Gets the API client.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IApiClient.</returns>
        IApiClient GetApiClient(IHasServerId item);

        /// <summary>
        /// Gets the API client.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>IApiClient.</returns>
        IApiClient GetApiClient(string serverId);
        
        /// <summary>
        /// Connects the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connects the specified API client.
        /// </summary>
        /// <param name="apiClient">The API client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(IApiClient apiClient, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Connects the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(ServerInfo server, CancellationToken cancellationToken = default);

        /// <summary>
        /// Connects the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(ServerInfo server, ConnectionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Connects the specified server.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(Uri address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task Logout();

        /// <summary>
        /// Gets the active api client instance
        /// </summary>
        IApiClient CurrentApiClient { get; }

        /// <summary>
        /// Gets the server information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;ServerInfo&gt;.</returns>
        Task<ServerInfo> GetServerInfo(string id);

        /// <summary>
        /// Gets the available servers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<ServerInfo>> GetAvailableServers(CancellationToken cancellationToken = default);
    }
}
