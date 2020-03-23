namespace Jellyfin.ApiClient.Model
{
    public interface IDevice
    {
        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        string DeviceName { get; }
        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        string DeviceId { get; }
    }
}
