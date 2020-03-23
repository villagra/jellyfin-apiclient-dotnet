using MediaBrowser.Model.Extensions;
using System;
using System.Collections.Generic;

namespace Jellyfin.ApiClient.Model
{
    public class ServerCredentials
    {
        public List<ServerInfo> Servers { get; set; }
        public string ConnectUserId { get; set; }
        public string ConnectAccessToken { get; set; }

        public ServerCredentials()
        {
            Servers = new List<ServerInfo>();
        }

        public void AddOrUpdateServer(ServerInfo server)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            // Clone the existing list of servers
            var list = new List<ServerInfo>();
            foreach (ServerInfo serverInfo in Servers)
            {
                list.Add(serverInfo);
            }

            var index = FindIndex(list, server.Id);

            if (index != -1)
            {
                var existing = list[index];

                // Take the most recent DateLastAccessed
                if (server.DateLastAccessed > existing.DateLastAccessed)
                {
                    existing.DateLastAccessed = server.DateLastAccessed;
                }

                if (!string.IsNullOrEmpty(server.AccessToken))
                {
                    existing.AccessToken = server.AccessToken;
                    existing.UserId = server.UserId;
                }
                if (!string.IsNullOrEmpty(server.Address.ToString()))
                {
                    existing.Address = server.Address;
                }
                if (!string.IsNullOrEmpty(server.Name))
                {
                    existing.Name = server.Name;
                }
            }
            else
            {
                list.Add(server);
            }

            Servers = list;
        }

        private int FindIndex(List<ServerInfo> servers, string id)
        {
            var index = 0;

            foreach (ServerInfo server in servers)
            {
                if (StringHelper.EqualsIgnoreCase(id, server.Id))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public ServerInfo GetServer(string id)
        {
            foreach (ServerInfo server in Servers)
            {
                if (StringHelper.EqualsIgnoreCase(id, server.Id))
                {
                    return server;
                }
            }

            return null;
        }
    }
}
