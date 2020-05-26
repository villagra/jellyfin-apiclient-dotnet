using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class Person
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Role { get; set; }
        public string Type { get; set; }
        public string PrimaryImageTag { get; set; }
    }
}
