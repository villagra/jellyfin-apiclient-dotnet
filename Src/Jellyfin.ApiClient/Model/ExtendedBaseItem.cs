using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class ExtendedBaseItem : BaseItem
    {
        public UserItemDataDto UserData { get; set; }
        public Dictionary<MediaBrowser.Model.Entities.ImageType, string> ImageTags { get; set; }
        public string[] BackdropImageTags { get; set; }
    }
}
