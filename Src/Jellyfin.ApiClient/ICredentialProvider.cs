using Jellyfin.ApiClient.Model;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient
{
    public interface ICredentialProvider
    {
        /// <summary>
        /// Gets the server credentials.
        /// </summary>
        /// <returns>ServerCredentialConfiguration.</returns>
        Task<ServerCredentials> GetServerCredentials();

        /// <summary>
        /// Saves the server credentials.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        Task SaveServerCredentials(ServerCredentials configuration);
    }
}
