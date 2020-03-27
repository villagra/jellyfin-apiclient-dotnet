using Jellyfin.ApiClient.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    [JsonConverter(typeof(ItemConverter))]
    public class BaseItem
    {
        public string Id { get; set; }
    }
}
