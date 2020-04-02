using Jellyfin.ApiClient.Serialization;
using MediaBrowser.Model.Dto;
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
        public String Name { get; set; }
        public String ServerId { get; set; }
        public UserItemDataDto UserData { get; set; }
        public Dictionary<MediaBrowser.Model.Entities.ImageType, string> ImageTags { get; set; }
        public string[] BackdropImageTags { get; set; }
    }
}
