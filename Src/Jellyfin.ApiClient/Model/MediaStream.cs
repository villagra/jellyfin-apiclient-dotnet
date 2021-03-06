﻿namespace Jellyfin.ApiClient.Model
{
    public class MediaStream
    {
        public string DisplayTitle { get; set; }
        public string Codec { get; set; }
        public string Language { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
        public string DeliveryUrl { get; set; }
        public bool IsExternal { get; set; }
        public bool IsTextSubtitleStream { get; set; }
    }
}