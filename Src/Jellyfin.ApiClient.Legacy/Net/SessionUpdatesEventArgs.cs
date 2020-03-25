using Jellyfin.ApiClient.Model;

namespace Jellyfin.ApiClient.Net
{
    /// <summary>	
    /// Class SessionUpdatesEventArgs	
    /// </summary>	
    public class SessionUpdatesEventArgs
    {
        public SessionInfoDto[] Sessions { get; set; }
    }
}
