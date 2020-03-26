using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Auth
{
    public class UserAuthentication : IAuthenticationMethod
    {
        public string ClientName { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string ApplicationVersion { get; set; }

        public UserAuthentication(string clientName, string deviceName, string deviceId, string applicationVersion) 
        {
            ClientName = String.IsNullOrWhiteSpace(clientName) ? throw new ArgumentException(nameof(clientName)) : clientName;
            DeviceName = String.IsNullOrWhiteSpace(deviceName) ? throw new ArgumentException(nameof(deviceName)) : deviceName;
            DeviceId = String.IsNullOrWhiteSpace(deviceId) ? throw new ArgumentException(nameof(deviceId)) : deviceId;
            ApplicationVersion = String.IsNullOrWhiteSpace(applicationVersion) ? throw new ArgumentException(nameof(applicationVersion)) : applicationVersion;
        }
    }
}
