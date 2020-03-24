using MediaBrowser.Model.System;
using System;

namespace Jellyfin.ApiClient.Model
{
    public class ServerInfo
    {
        public String Name { get; set; }
        public String Id { get; set; }
        public Uri Address { get; set; }
        public Guid UserId { get; set; }
        public String AccessToken { get; set; }
        public DateTime DateLastAccessed { get; set; }

        public ServerInfo()
        {
        }

        public void ImportInfo(PublicSystemInfo systemInfo)
        {
            Name = systemInfo.ServerName;
            Id = systemInfo.Id;

            if (!string.IsNullOrEmpty(systemInfo.LocalAddress))
            {
                Address = new Uri(systemInfo.LocalAddress, UriKind.Relative);
            }

            if (!string.IsNullOrEmpty(systemInfo.LocalAddress))
            {
                Address = new Uri(systemInfo.LocalAddress, UriKind.Relative);
            }
        }
    }
}
