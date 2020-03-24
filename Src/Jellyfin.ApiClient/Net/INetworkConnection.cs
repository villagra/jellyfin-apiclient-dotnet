using Jellyfin.ApiClient.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Net
{
    public interface INetworkConnection
    {
        /// <summary>
        /// Occurs when [network changed].
        /// </summary>
        event EventHandler<EventArgs> NetworkChanged;

        /// <summary>
        /// Gets the network status.
        /// </summary>
        /// <returns>NetworkStatus.</returns>
        NetworkStatus GetNetworkStatus();

#if WINDOWS_UWP
        bool HasUnmeteredConnection();
#endif
    }
}
