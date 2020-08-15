using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class User
    {
        public string Name { get; set; }
        public string ServerId { get; set; }
        public string ServerName { get; set; }
        public string Id { get; set; }
        public string PrimaryImageTag { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}
