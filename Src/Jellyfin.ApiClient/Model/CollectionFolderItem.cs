using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class CollectionFolderItem : BaseItem
    { 
        public string Etag { get; set; }
        public DateTime DateCreated { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public string SortName { get; set; }
        public List<object> ExternalUrls { get; set; }
        public string Path { get; set; }
        public bool EnableMediaSourceDisplay { get; set; }
        public List<object> Taglines { get; set; }
        public List<object> Genres { get; set; }
        public string PlayAccess { get; set; }
        public List<object> RemoteTrailers { get; set; }
        public Dictionary<string, string> ProviderIds { get; set; }
        public bool IsFolder { get; set; }
        public string ParentId { get; set; }
        public string Type { get; set; }
        public List<object> People { get; set; }
        public List<object> Studios { get; set; }
        public List<object> GenreItems { get; set; }
        public int LocalTrailerCount { get; set; }
        public int ChildCount { get; set; }
        public int SpecialFeatureCount { get; set; }
        public string DisplayPreferencesId { get; set; }
        public List<object> Tags { get; set; }
        public int PrimaryImageAspectRatio { get; set; }
        public string CollectionType { get; set; }
        public Dictionary<ImageType, string> ImageTags { get; set; }
        public List<object> BackdropImageTags { get; set; }
        public List<object> ScreenshotImageTags { get; set; }
        public string LocationType { get; set; }
        public List<object> LockedFields { get; set; }
        public bool LockData { get; set; }

    }
}
