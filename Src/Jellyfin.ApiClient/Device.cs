using Jellyfin.ApiClient.Model;

namespace Jellyfin.ApiClient
{
    public class Device : IDevice
    {
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
    }
}
