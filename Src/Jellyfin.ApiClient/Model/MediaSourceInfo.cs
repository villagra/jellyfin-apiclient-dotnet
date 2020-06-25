namespace Jellyfin.ApiClient.Model
{
    public class MediaSourceInfo
    {
        public string Id { get; set; }
        public string Container { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public MediaStream[] MediaStreams { get; set; }
        public string DirectStreamUrl { get; set; }
    }
}