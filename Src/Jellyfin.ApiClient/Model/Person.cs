using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class Person : BaseItem
    {
        public string Role { get; set; }
        public string Type { get; set; }
        public string Overview { get; set; }
        public DateTime? PremiereDate { get; set; }
        public string[] ProductionLocations { get; set; }
        public string PrimaryImageTag { get; set; }
    }
}
